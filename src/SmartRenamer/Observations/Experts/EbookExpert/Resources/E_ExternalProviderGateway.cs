using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SmartRenamer.Observations.Experts.EbookExpert.Resources
{
    /// <summary>
    /// Shared, provider-aware HTTP gateway for EbookExpert external research.
    ///
    /// This is deliberately the single place where Scout decides how quickly
    /// it talks to a provider. The research resources decide what to ask for;
    /// this gateway decides how safely to ask it.
    ///
    /// Guarantees:
    ///   - one conservative request cadence per provider;
    ///   - provider-specific cooldown after HTTP 429;
    ///   - bounded retry with backoff for transient 429/5xx responses;
    ///   - Retry-After is respected when supplied;
    ///   - successful responses are cached briefly so the same URL is not
    ///     repeatedly requested during an expedition;
    ///   - Open Library identifies Scout when a contact value is configured;
    ///   - Google Books and Open Library are throttled independently.
    ///
    /// It intentionally does NOT rotate providers itself. Provider selection
    /// remains the responsibility of the research resource. This prevents a
    /// provider failure from accidentally becoming a burst against another
    /// provider.
    /// </summary>
    internal static class E_ExternalProviderGateway
    {
        private static readonly HttpClient HttpClient = CreateHttpClient();

        private static readonly object StateLock = new();

        private static readonly Dictionary<string, ProviderState> States =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, CacheEntry> Cache =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly TimeSpan CacheLifetime =
            TimeSpan.FromMinutes(20);

        private const int MaxCacheEntries = 256;

        private const int MaxAttempts = 2;

        private const int DefaultGoogleMinimumIntervalMilliseconds = 1000;

        private const int DefaultOpenLibraryMinimumIntervalMilliseconds = 1100;

        private const int IdentifiedOpenLibraryMinimumIntervalMilliseconds = 1000;

        private const int Maximum429WaitMilliseconds = 10000;

        private const int FinalRateLimitCooldownMilliseconds = 15000;

        public static E_ExternalProviderFetchResult FetchJson(
            string provider,
            string url)
        {
            if (string.IsNullOrWhiteSpace(provider))
                throw new ArgumentException(
                    "A provider name is required.",
                    nameof(provider));

            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException(
                    "A provider URL is required.",
                    nameof(url));

            if (TryGetCached(url, out string? cachedJson))
            {
                return new E_ExternalProviderFetchResult
                {
                    Status = E_ExternalProviderFetchStatus.Success,
                    Json = cachedJson,
                    Diagnostic =
                        $"{provider} response served from Scout's research cache."
                };
            }

            int minimumIntervalMilliseconds =
                GetMinimumIntervalMilliseconds(provider);

            DateTimeOffset? finalRateLimitUntil = null;
            string? lastRateLimitDiagnostic = null;

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                WaitForProvider(
                    provider,
                    minimumIntervalMilliseconds);

                try
                {
                    using HttpRequestMessage request =
                        new(HttpMethod.Get, url);

                    request.Headers.Accept.Clear();
                    request.Headers.Accept.Add(
                        new MediaTypeWithQualityHeaderValue(
                            "application/json"));

                    AddProviderHeaders(request, provider);

                    using HttpResponseMessage response =
                        HttpClient.Send(
                            request,
                            HttpCompletionOption.ResponseContentRead,
                            CancellationToken.None);

                    if (response.IsSuccessStatusCode)
                    {
                        string json =
                            response.Content
                                .ReadAsStringAsync()
                                .GetAwaiter()
                                .GetResult();

                        if (string.IsNullOrWhiteSpace(json))
                        {
                            return new E_ExternalProviderFetchResult
                            {
                                Status = E_ExternalProviderFetchStatus.Empty,
                                Diagnostic =
                                    $"{provider} responded successfully but returned an empty body."
                            };
                        }

                        StoreCache(url, json);

                        return new E_ExternalProviderFetchResult
                        {
                            Status = E_ExternalProviderFetchStatus.Success,
                            Json = json,
                            Diagnostic =
                                $"{provider} responded successfully."
                        };
                    }

                    if ((int)response.StatusCode == 429)
                    {
                        TimeSpan retryDelay =
                            GetRetryDelay(response, attempt);

                        DateTimeOffset cooldownUntil =
                            DateTimeOffset.UtcNow + retryDelay;

                        SetCooldown(
                            provider,
                            cooldownUntil);

                        lastRateLimitDiagnostic =
                            BuildRateLimitDiagnostic(
                                provider,
                                response,
                                retryDelay);

                        finalRateLimitUntil = cooldownUntil;

                        // A short Retry-After is safe to honor once. A long
                        // Retry-After is deliberately not waited out on the
                        // foreground research call; the provider is cooled
                        // down and another provider can be tried instead.
                        if (attempt + 1 < MaxAttempts &&
                            retryDelay.TotalMilliseconds <= Maximum429WaitMilliseconds)
                        {
                            Thread.Sleep(retryDelay);
                            continue;
                        }

                        break;
                    }

                    if ((int)response.StatusCode >= 500 &&
                        attempt + 1 < MaxAttempts)
                    {
                        TimeSpan retryDelay =
                            GetTransientRetryDelay(attempt);

                        SetCooldown(
                            provider,
                            DateTimeOffset.UtcNow + retryDelay);

                        Thread.Sleep(retryDelay);
                        continue;
                    }

                    return new E_ExternalProviderFetchResult
                    {
                        Status = E_ExternalProviderFetchStatus.Unavailable,
                        Diagnostic =
                            $"{provider} responded with HTTP {(int)response.StatusCode} ({response.ReasonPhrase ?? "no reason"})."
                    };
                }
                catch (TaskCanceledException)
                {
                    return new E_ExternalProviderFetchResult
                    {
                        Status = E_ExternalProviderFetchStatus.TimedOut,
                        Diagnostic =
                            $"{provider} timed out after Scout's research limit."
                    };
                }
                catch (HttpRequestException exception)
                {
                    return new E_ExternalProviderFetchResult
                    {
                        Status = E_ExternalProviderFetchStatus.Unavailable,
                        Diagnostic =
                            $"{provider} could not be reached: {exception.Message}"
                    };
                }
                catch (Exception exception)
                {
                    return new E_ExternalProviderFetchResult
                    {
                        Status = E_ExternalProviderFetchStatus.Unavailable,
                        Diagnostic =
                            $"{provider} research failed unexpectedly: {exception.Message}"
                    };
                }
            }

            DateTimeOffset finalCooldown =
                DateTimeOffset.UtcNow.AddMilliseconds(
                    FinalRateLimitCooldownMilliseconds);

            if (finalRateLimitUntil.HasValue &&
                finalRateLimitUntil.Value > finalCooldown)
            {
                finalCooldown = finalRateLimitUntil.Value;
            }

            SetCooldown(
                provider,
                finalCooldown);

            TimeSpan retryAfter =
                finalCooldown - DateTimeOffset.UtcNow;

            return new E_ExternalProviderFetchResult
            {
                Status = E_ExternalProviderFetchStatus.RateLimited,
                RetryAfterSeconds =
                    Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)),
                Diagnostic =
                    lastRateLimitDiagnostic ??
                    $"{provider} is temporarily rate limited. Scout will not immediately send another request to this provider."
            };
        }

        private static HttpClient CreateHttpClient()
        {
            HttpClient client = new()
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(
                    "Scout-EbookExpert",
                    "1.0"));

            return client;
        }

        private static void AddProviderHeaders(
            HttpRequestMessage request,
            string provider)
        {
            if (!provider.Equals(
                    "Open Library",
                    StringComparison.OrdinalIgnoreCase))
                return;

            string? contact =
                Environment.GetEnvironmentVariable(
                    "SCOUT_OPENLIBRARY_CONTACT");

            if (string.IsNullOrWhiteSpace(contact))
                return;

            request.Headers.UserAgent.Clear();
            request.Headers.UserAgent.Add(
                new ProductInfoHeaderValue(
                    "Scout-EbookExpert",
                    "1.0"));
            request.Headers.UserAgent.ParseAdd(
                $"({contact.Trim()})");
        }

        private static int GetMinimumIntervalMilliseconds(
            string provider)
        {
            if (provider.Equals(
                    "Open Library",
                    StringComparison.OrdinalIgnoreCase))
            {
                string? contact =
                    Environment.GetEnvironmentVariable(
                        "SCOUT_OPENLIBRARY_CONTACT");

                return string.IsNullOrWhiteSpace(contact)
                    ? DefaultOpenLibraryMinimumIntervalMilliseconds
                    : IdentifiedOpenLibraryMinimumIntervalMilliseconds;
            }

            if (provider.Equals(
                    "Google Books",
                    StringComparison.OrdinalIgnoreCase))
            {
                return DefaultGoogleMinimumIntervalMilliseconds;
            }

            return DefaultGoogleMinimumIntervalMilliseconds;
        }

        private static void WaitForProvider(
            string provider,
            int minimumIntervalMilliseconds)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset waitUntil;

            lock (StateLock)
            {
                if (!States.TryGetValue(provider, out ProviderState? state))
                {
                    state = new ProviderState();
                    States[provider] = state;
                }

                waitUntil = state.NextAllowedUtc > state.CooldownUntilUtc
                    ? state.NextAllowedUtc
                    : state.CooldownUntilUtc;
            }

            if (waitUntil > now)
            {
                TimeSpan delay = waitUntil - now;
                Thread.Sleep(delay);
            }

            lock (StateLock)
            {
                if (!States.TryGetValue(provider, out ProviderState? state))
                {
                    state = new ProviderState();
                    States[provider] = state;
                }

                DateTimeOffset latest = DateTimeOffset.UtcNow;
                DateTimeOffset next =
                    latest.AddMilliseconds(minimumIntervalMilliseconds);

                state.LastRequestUtc = latest;
                state.NextAllowedUtc = next;
            }
        }

        private static void SetCooldown(
            string provider,
            DateTimeOffset until)
        {
            lock (StateLock)
            {
                if (!States.TryGetValue(provider, out ProviderState? state))
                {
                    state = new ProviderState();
                    States[provider] = state;
                }

                if (until > state.CooldownUntilUtc)
                    state.CooldownUntilUtc = until;
            }
        }

        private static TimeSpan GetRetryDelay(
            HttpResponseMessage response,
            int attempt)
        {
            if (response.Headers.RetryAfter != null)
            {
                if (response.Headers.RetryAfter.Delta.HasValue)
                {
                    TimeSpan delta =
                        response.Headers.RetryAfter.Delta.Value;

                    if (delta >= TimeSpan.Zero)
                        return delta;
                }

                if (response.Headers.RetryAfter.Date.HasValue)
                {
                    TimeSpan until =
                        response.Headers.RetryAfter.Date.Value -
                        DateTimeOffset.UtcNow;

                    if (until >= TimeSpan.Zero)
                        return until;
                }
            }

            return TimeSpan.FromSeconds(
                attempt == 0 ? 2 : 4);
        }

        private static TimeSpan GetTransientRetryDelay(int attempt)
        {
            return TimeSpan.FromSeconds(
                attempt == 0 ? 2 : 4);
        }

        private static string BuildRateLimitDiagnostic(
            string provider,
            HttpResponseMessage response,
            TimeSpan retryDelay)
        {
            string retryText =
                retryDelay > TimeSpan.Zero
                    ? $" Scout will wait about {Math.Ceiling(retryDelay.TotalSeconds):0} second(s) before another attempt."
                    : " Scout will not immediately send another request.";

            return
                $"{provider} responded with HTTP 429 (Too Many Requests)." +
                retryText;
        }

        private static bool TryGetCached(
            string url,
            out string? json)
        {
            lock (StateLock)
            {
                if (Cache.TryGetValue(url, out CacheEntry? entry))
                {
                    if (entry.ExpiresUtc > DateTimeOffset.UtcNow)
                    {
                        json = entry.Json;
                        return true;
                    }

                    Cache.Remove(url);
                }
            }

            json = null;
            return false;
        }

        private static void StoreCache(
            string url,
            string json)
        {
            lock (StateLock)
            {
                Cache[url] = new CacheEntry
                {
                    Json = json,
                    ExpiresUtc = DateTimeOffset.UtcNow + CacheLifetime
                };

                if (Cache.Count <= MaxCacheEntries)
                    return;

                string? oldestKey = null;
                DateTimeOffset oldest = DateTimeOffset.MaxValue;

                foreach (KeyValuePair<string, CacheEntry> item in Cache)
                {
                    if (item.Value.ExpiresUtc < oldest)
                    {
                        oldest = item.Value.ExpiresUtc;
                        oldestKey = item.Key;
                    }
                }

                if (oldestKey != null)
                    Cache.Remove(oldestKey);
            }
        }

        private sealed class ProviderState
        {
            public DateTimeOffset LastRequestUtc { get; set; }

            public DateTimeOffset NextAllowedUtc { get; set; }

            public DateTimeOffset CooldownUntilUtc { get; set; }
        }

        private sealed class CacheEntry
        {
            public string Json { get; init; } = string.Empty;

            public DateTimeOffset ExpiresUtc { get; init; }
        }
    }

    internal enum E_ExternalProviderFetchStatus
    {
        Success,
        Empty,
        RateLimited,
        TimedOut,
        Unavailable
    }

    internal sealed class E_ExternalProviderFetchResult
    {
        public E_ExternalProviderFetchStatus Status { get; init; }

        public string? Json { get; init; }

        public string Diagnostic { get; init; } = string.Empty;

        public int RetryAfterSeconds { get; init; }

        public bool Succeeded =>
            Status == E_ExternalProviderFetchStatus.Success;

        public bool RateLimited =>
            Status == E_ExternalProviderFetchStatus.RateLimited;

        public bool TimedOut =>
            Status == E_ExternalProviderFetchStatus.TimedOut;

        public bool Responded =>
            Status == E_ExternalProviderFetchStatus.Success ||
            Status == E_ExternalProviderFetchStatus.Empty ||
            Status == E_ExternalProviderFetchStatus.RateLimited;
    }
}
