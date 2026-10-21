using Scout.Observations.Experts.EbookExpert.Data;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Net;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SmartRenamer.Observations.Experts.EbookExpert.Resources
{
    /// <summary>
    /// Researches missing text metadata using the same external evidence
    /// source already used by EbookExpert for ISBN recovery.
    ///
    /// This resource only gathers candidates. It never chooses a value,
    /// approves a repair, modifies an EPUB, or communicates with the UI.
    /// </summary>
    internal sealed class E_MetadataResearchResource
    {
        public MetadataResearchResult Research(
            E_EbookMetadata metadata,
            IReadOnlyList<MetadataEvidence> evidence)
        {
            ArgumentNullException.ThrowIfNull(metadata);
            ArgumentNullException.ThrowIfNull(evidence);

            string title = GetValue(metadata.Title, evidence, "Title");
            string author = GetValue(metadata.Author, evidence, "Author");
            string publisher = GetValue(metadata.Publisher, evidence, "Publisher");
            string isbn = GetValue(metadata.Isbn, evidence, "ISBN");
            string openingContent = string.Join("\n", evidence
                .Where(item => string.Equals(item.Source, "Opening Content", StringComparison.OrdinalIgnoreCase) &&
                               string.Equals(item.Field, "Content", StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Take(10));

            if (string.IsNullOrWhiteSpace(title) &&
                string.IsNullOrWhiteSpace(author) &&
                string.IsNullOrWhiteSpace(isbn))
            {
                return new MetadataResearchResult();
            }

            try
            {
                List<MetadataResearchCandidate> candidates = new();
                bool providerResponded = false;
                bool providerSucceeded = false;
                bool anyRateLimited = false;
                bool anyTimedOut = false;
                int retryAfterSeconds = 0;
                List<string> diagnostics = new();

                // Open Library remains the first source because Scout already
                // uses it for ISBN research. Before making a new provider call,
                // consume the canonical Title/Author response if an earlier
                // ISBN research operation already collected it. This is a cache-
                // only probe: it never waits, retries, or contacts the provider.
                string identityOpenLibraryUrl =
                    BuildOpenLibraryIdentitySearchUrl(title, author);

                bool openLibraryEvidenceSatisfiesRequest = false;
                string? openLibrarySearchJson = null;

                if (E_ExternalProviderGateway.TryGetCachedJson(
                        identityOpenLibraryUrl,
                        out string? cachedOpenLibraryJson) &&
                    !string.IsNullOrWhiteSpace(cachedOpenLibraryJson))
                {
                    openLibrarySearchJson = cachedOpenLibraryJson;
                    List<MetadataResearchCandidate> cachedOpenLibraryCandidates =
                        ParseOpenLibraryCandidates(
                            cachedOpenLibraryJson!,
                            metadata,
                            identityOpenLibraryUrl);

                    candidates.AddRange(cachedOpenLibraryCandidates);

                    // Cached provider evidence is still successful external
                    // evidence. It must not be reported as ProviderUnavailable
                    // merely because no new HTTP request was necessary.
                    providerResponded = true;
                    providerSucceeded = true;

                    Debug.WriteLine(
                        $"[METADATA CACHE TRACE] Open Library cache hit | " +
                        $"Title='{title}' | Author='{author}' | " +
                        $"Candidates={cachedOpenLibraryCandidates.Count} | " +
                        $"Description={cachedOpenLibraryCandidates.Any(c => string.Equals(c.Field, "Description", StringComparison.OrdinalIgnoreCase))} | " +
                        $"Publisher={cachedOpenLibraryCandidates.Any(c => string.Equals(c.Field, "Publisher", StringComparison.OrdinalIgnoreCase))}");

                    bool cachedDescriptionIsFull =
                        candidates.Any(candidate =>
                            string.Equals(
                                candidate.Field,
                                "Description",
                                StringComparison.OrdinalIgnoreCase) &&
                            !candidate.Details.Any(detail =>
                                detail.Contains(
                                    "first-sentence summary rather than a full synopsis",
                                    StringComparison.OrdinalIgnoreCase)));

                    bool cachedDescriptionIsFirstSentenceOnly =
                        candidates.Any(candidate =>
                            string.Equals(
                                candidate.Field,
                                "Description",
                                StringComparison.OrdinalIgnoreCase) &&
                            candidate.Details.Any(detail =>
                                detail.Contains(
                                    "first-sentence summary rather than a full synopsis",
                                    StringComparison.OrdinalIgnoreCase)));

                    openLibraryEvidenceSatisfiesRequest =
                        (!string.IsNullOrWhiteSpace(metadata.Publisher) ||
                         candidates.Any(candidate =>
                             string.Equals(
                                 candidate.Field,
                                 "Publisher",
                                 StringComparison.OrdinalIgnoreCase))) &&
                        (!string.IsNullOrWhiteSpace(metadata.Description) ||
                         cachedDescriptionIsFull);

                    Debug.WriteLine(
                        $"[METADATA SUFFICIENCY TRACE] Open Library cache | " +
                        $"Title='{title}' | FullDescription={cachedDescriptionIsFull} | " +
                        $"FirstSentenceOnly={cachedDescriptionIsFirstSentenceOnly} | " +
                        $"SatisfiesRequest={openLibraryEvidenceSatisfiesRequest}");

                    diagnostics.Add(
                        openLibraryEvidenceSatisfiesRequest
                            ? "Open Library identity response reused from Scout's research cache; no new Open Library request was made for this evidence."
                            : "Open Library identity response was reused from Scout's research cache, but it did not contain all requested metadata; Scout will continue with the edition-aware lookup.");
                }

                if (!openLibraryEvidenceSatisfiesRequest)
                {
                    string openLibraryUrl =
                        BuildOpenLibrarySearchUrl(title, author, isbn);

                    E_ExternalProviderFetchResult openLibrary =
                        TryFetchJson(openLibraryUrl, "Open Library");
                    TraceProviderFetch("Open Library search", openLibraryUrl, openLibrary);

                    providerResponded |= openLibrary.Responded;
                    providerSucceeded |= openLibrary.Succeeded;
                    anyRateLimited |= openLibrary.RateLimited;
                    anyTimedOut |= openLibrary.TimedOut;
                    retryAfterSeconds = Math.Max(
                        retryAfterSeconds,
                        openLibrary.RetryAfterSeconds);
                    AddDiagnostic(diagnostics, openLibrary.Diagnostic);

                    if (openLibrary.Succeeded &&
                        !string.IsNullOrWhiteSpace(openLibrary.Json))
                    {
                        openLibrarySearchJson = openLibrary.Json;
                        List<MetadataResearchCandidate> fetchedOpenLibraryCandidates =
                            ParseOpenLibraryCandidates(
                                openLibrary.Json!,
                                metadata,
                                openLibraryUrl);

                        candidates.AddRange(fetchedOpenLibraryCandidates);

                        Debug.WriteLine(
                            $"[METADATA CANDIDATE TRACE] Open Library | " +
                            $"Title='{title}' | Candidates={fetchedOpenLibraryCandidates.Count}");

                        // An ISBN lookup is edition-specific. If it produces no
                        // usable metadata candidates, retry once using the
                        // canonical title+author identity query. Do not append
                        // the failed ISBN or noisy series/user clues to this
                        // fallback: alternate editions legitimately have other
                        // ISBNs, and the work may still have useful metadata.
                        if (fetchedOpenLibraryCandidates.Count == 0 &&
                            !string.IsNullOrWhiteSpace(isbn) &&
                            !string.IsNullOrWhiteSpace(title) &&
                            !string.IsNullOrWhiteSpace(author))
                        {
                            E_ExternalProviderFetchResult identityFallback =
                                TryFetchJson(identityOpenLibraryUrl, "Open Library");
                            TraceProviderFetch(
                                "Open Library title-author fallback",
                                identityOpenLibraryUrl,
                                identityFallback);

                            providerResponded |= identityFallback.Responded;
                            providerSucceeded |= identityFallback.Succeeded;
                            anyRateLimited |= identityFallback.RateLimited;
                            anyTimedOut |= identityFallback.TimedOut;
                            retryAfterSeconds = Math.Max(
                                retryAfterSeconds,
                                identityFallback.RetryAfterSeconds);
                            AddDiagnostic(diagnostics, identityFallback.Diagnostic);
                            diagnostics.Add(
                                "Open Library ISBN search produced no usable metadata candidates; Scout retried once using title plus author to find alternate editions of the same work.");

                            if (identityFallback.Succeeded &&
                                !string.IsNullOrWhiteSpace(identityFallback.Json))
                            {
                                openLibrarySearchJson = identityFallback.Json;
                                List<MetadataResearchCandidate> identityCandidates =
                                    ParseOpenLibraryCandidates(
                                        identityFallback.Json!,
                                        metadata,
                                        identityOpenLibraryUrl);
                                candidates.AddRange(identityCandidates);

                                Debug.WriteLine(
                                    $"[METADATA CANDIDATE TRACE] Open Library title-author fallback | " +
                                    $"Title='{title}' | Candidates={identityCandidates.Count}");
                            }
                        }
                    }
                }

                // If Open Library search returned only a first sentence, follow
                // the best matching Work record once. Search results often expose
                // first_sentence even when the Work endpoint has a richer
                // description. Keep this bounded to one additional request and
                // use the same gateway for caching/throttling.
                bool hasFullDescription = candidates.Any(candidate =>
                    string.Equals(candidate.Field, "Description", StringComparison.OrdinalIgnoreCase) &&
                    !candidate.Details.Any(detail => detail.Contains(
                        "first-sentence summary rather than a full synopsis",
                        StringComparison.OrdinalIgnoreCase)));

                if (string.IsNullOrWhiteSpace(metadata.Description) &&
                    !hasFullDescription &&
                    !string.IsNullOrWhiteSpace(title) &&
                    !string.IsNullOrWhiteSpace(author))
                {
                    string? workUrl = BuildBestOpenLibraryWorkUrl(
                        openLibrarySearchJson,
                        title,
                        author);

                    Debug.WriteLine(
                        $"[METADATA WORK LOOKUP TRACE] Title='{title}' | " +
                        $"SelectedWorkUrl='{workUrl ?? "(no sufficiently matching Work key)"}'");

                    if (!string.IsNullOrWhiteSpace(workUrl))
                    {
                        E_ExternalProviderFetchResult workResult =
                            TryFetchJson(workUrl, "Open Library");
                        TraceProviderFetch("Open Library Work", workUrl, workResult);

                        providerResponded |= workResult.Responded;
                        providerSucceeded |= workResult.Succeeded;
                        anyRateLimited |= workResult.RateLimited;
                        anyTimedOut |= workResult.TimedOut;
                        retryAfterSeconds = Math.Max(
                            retryAfterSeconds,
                            workResult.RetryAfterSeconds);
                        AddDiagnostic(diagnostics, workResult.Diagnostic);

                        if (workResult.Succeeded &&
                            !string.IsNullOrWhiteSpace(workResult.Json))
                        {
                            MetadataResearchCandidate? workDescription =
                                ParseOpenLibraryWorkDescription(
                                    workResult.Json!, metadata, workUrl);

                            if (workDescription != null)
                            {
                                candidates.Add(workDescription);
                                hasFullDescription = true;
                                Debug.WriteLine(
                                    $"[METADATA CANDIDATE TRACE] Open Library Work | " +
                                    $"Title='{title}' | DescriptionLength={workDescription.Value.Length} | " +
                                    $"Confidence={workDescription.Confidence:P0}");
                            }
                        }
                    }
                }

                // Google Books remains the final fallback. It is used only
                // when requested metadata is still missing. Each provider is
                // throttled independently by the gateway.
                bool needGoogle =
                    (string.IsNullOrWhiteSpace(metadata.Description) &&
                     !HasFullDescriptionCandidate(candidates)) ||
                    (string.IsNullOrWhiteSpace(metadata.Publisher) &&
                     !candidates.Any(candidate =>
                         string.Equals(candidate.Field, "Publisher", StringComparison.OrdinalIgnoreCase)));

                if (needGoogle)
                {
                    // Google Books uses the same canonical Title/Author query
                    // for ISBN recovery. Reuse that response first when it is
                    // already cached; only fall back to the current ISBN-aware
                    // query when the needed evidence is not already present.
                    string identityGoogleUrl =
                        BuildGoogleBooksIdentitySearchUrl(title, author);

                    bool googleCacheSatisfiedRequest = false;

                    if (E_ExternalProviderGateway.TryGetCachedJson(
                            identityGoogleUrl,
                            out string? cachedGoogleJson) &&
                        !string.IsNullOrWhiteSpace(cachedGoogleJson))
                    {
                        List<MetadataResearchCandidate> cachedGoogleCandidates =
                            ParseGoogleBooksCandidates(
                                cachedGoogleJson!,
                                metadata,
                                identityGoogleUrl);

                        candidates.AddRange(cachedGoogleCandidates);

                        // Cached Google Books evidence is valid external
                        // evidence and must count as a successful research
                        // response for the overall result state.
                        providerResponded = true;
                        providerSucceeded = true;

                        Debug.WriteLine(
                            $"[METADATA CACHE TRACE] Google Books cache hit | " +
                            $"Title='{title}' | Author='{author}' | " +
                            $"Candidates={cachedGoogleCandidates.Count} | " +
                            $"Description={cachedGoogleCandidates.Any(c => string.Equals(c.Field, "Description", StringComparison.OrdinalIgnoreCase))} | " +
                            $"Publisher={cachedGoogleCandidates.Any(c => string.Equals(c.Field, "Publisher", StringComparison.OrdinalIgnoreCase))}");

                        googleCacheSatisfiedRequest =
                            (!string.IsNullOrWhiteSpace(metadata.Description) ||
                             HasFullDescriptionCandidate(candidates)) &&
                            (!string.IsNullOrWhiteSpace(metadata.Publisher) ||
                             candidates.Any(candidate =>
                                 string.Equals(
                                     candidate.Field,
                                     "Publisher",
                                     StringComparison.OrdinalIgnoreCase)));

                        diagnostics.Add(
                            googleCacheSatisfiedRequest
                                ? "Google Books identity response reused from Scout's research cache; no new Google Books request was made for this evidence."
                                : "Google Books identity response was reused from Scout's research cache, but it did not contain all requested metadata; Scout will continue with the current lookup.");
                    }

                    if (!googleCacheSatisfiedRequest)
                    {
                        string googleUrl =
                            BuildGoogleBooksSearchUrl(title, author, isbn);

                        E_ExternalProviderFetchResult google =
                            TryFetchJson(googleUrl, "Google Books");
                        TraceProviderFetch("Google Books primary", googleUrl, google);

                        providerResponded |= google.Responded;
                        providerSucceeded |= google.Succeeded;
                        anyRateLimited |= google.RateLimited;
                        anyTimedOut |= google.TimedOut;
                        retryAfterSeconds = Math.Max(
                            retryAfterSeconds,
                            google.RetryAfterSeconds);
                        AddDiagnostic(diagnostics, google.Diagnostic);

                        if (google.Succeeded &&
                            !string.IsNullOrWhiteSpace(google.Json))
                        {
                            List<MetadataResearchCandidate> fetchedGoogleCandidates =
                                ParseGoogleBooksCandidates(
                                    google.Json!,
                                    metadata,
                                    googleUrl);

                            candidates.AddRange(fetchedGoogleCandidates);

                            Debug.WriteLine(
                                $"[METADATA CANDIDATE TRACE] Google Books | " +
                                $"Title='{title}' | Candidates={fetchedGoogleCandidates.Count}");
                        }

                        // An ISBN-only search can miss a useful synopsis even
                        // when Google Books has a matching title/author volume.
                        // Permit one broader query only for that case, only if
                        // the first request was successful, and only while the
                        // description is still missing. Never double-query an
                        // already broad title/author search.
                        bool googleHasDescription = HasFullDescriptionCandidate(candidates);

                        if (string.IsNullOrWhiteSpace(metadata.Description) &&
                            !googleHasDescription &&
                            !string.IsNullOrWhiteSpace(isbn) &&
                            google.Succeeded &&
                            !google.RateLimited &&
                            !google.TimedOut &&
                            !string.IsNullOrWhiteSpace(title))
                        {
                            string broadGoogleUrl =
                                BuildGoogleBooksBroadSearchUrl(title, author);
                            E_ExternalProviderFetchResult broadGoogle =
                                TryFetchJson(broadGoogleUrl, "Google Books");
                            TraceProviderFetch("Google Books broad search", broadGoogleUrl, broadGoogle);

                            providerResponded |= broadGoogle.Responded;
                            providerSucceeded |= broadGoogle.Succeeded;
                            anyRateLimited |= broadGoogle.RateLimited;
                            anyTimedOut |= broadGoogle.TimedOut;
                            retryAfterSeconds = Math.Max(
                                retryAfterSeconds,
                                broadGoogle.RetryAfterSeconds);
                            AddDiagnostic(diagnostics, broadGoogle.Diagnostic);

                            if (broadGoogle.Succeeded &&
                                !string.IsNullOrWhiteSpace(broadGoogle.Json))
                            {
                                List<MetadataResearchCandidate> broadCandidates =
                                    ParseGoogleBooksCandidates(
                                        broadGoogle.Json!, metadata, broadGoogleUrl);
                                candidates.AddRange(broadCandidates);
                                Debug.WriteLine(
                                    $"[METADATA CANDIDATE TRACE] Google Books Broad Search | " +
                                    $"Title='{title}' | Candidates={broadCandidates.Count}");
                            }
                        }
                    }
                }

                // Internet Archive is retained only as a last-resort catalog
                // source. Catalog descriptions are not treated as synopsis
                // candidates; they may describe a scan/upload rather than the
                // book itself. Google Books and its broader identity query run
                // before this source for missing descriptions.
                bool needInternetArchive =
                    (string.IsNullOrWhiteSpace(metadata.Description) &&
                     !HasFullDescriptionCandidate(candidates)) ||
                    (string.IsNullOrWhiteSpace(metadata.Publisher) &&
                     !candidates.Any(candidate =>
                         string.Equals(candidate.Field, "Publisher", StringComparison.OrdinalIgnoreCase)));

                if (needInternetArchive &&
                    !string.IsNullOrWhiteSpace(title))
                {
                    string internetArchiveUrl =
                        BuildInternetArchiveSearchUrl(title, author);

                    E_ExternalProviderFetchResult internetArchive =
                        TryFetchJson(internetArchiveUrl, "Internet Archive");
                    TraceProviderFetch("Internet Archive catalog", internetArchiveUrl, internetArchive);

                    providerResponded |= internetArchive.Responded;
                    providerSucceeded |= internetArchive.Succeeded;
                    anyRateLimited |= internetArchive.RateLimited;
                    anyTimedOut |= internetArchive.TimedOut;
                    retryAfterSeconds = Math.Max(
                        retryAfterSeconds,
                        internetArchive.RetryAfterSeconds);
                    AddDiagnostic(diagnostics, internetArchive.Diagnostic);

                    if (internetArchive.Succeeded &&
                        !string.IsNullOrWhiteSpace(internetArchive.Json))
                    {
                        List<MetadataResearchCandidate> fetchedInternetArchiveCandidates =
                            ParseInternetArchiveCandidates(
                                internetArchive.Json!,
                                metadata,
                                internetArchiveUrl);

                        candidates.AddRange(fetchedInternetArchiveCandidates);

                        Debug.WriteLine(
                            $"[METADATA CANDIDATE TRACE] Internet Archive | " +
                            $"Title='{title}' | Candidates={fetchedInternetArchiveCandidates.Count}");
                    }
                }


                // Web discovery is the bounded fallback after structured catalogs.
                // It consumes the same local evidence and returns candidates only;
                // authorization and EPUB modification remain in E_ActionDispatcher.
                if (string.IsNullOrWhiteSpace(metadata.Description) &&
                    !HasFullDescriptionCandidate(candidates) &&
                    !string.IsNullOrWhiteSpace(title))
                {
                    try
                    {
                        List<MetadataResearchCandidate> webCandidates =
                            new E_WebConsultant().ResearchDescription(
                                title, author, publisher, isbn, openingContent);
                        candidates.AddRange(webCandidates);
                        if (webCandidates.Count > 0)
                        {
                            providerResponded = true;
                            providerSucceeded = true;
                        }
                        diagnostics.Add($"Web Consultant completed bounded discovery for '{title}' and returned {webCandidates.Count} description candidate(s).");
                    }
                    catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is InvalidOperationException)
                    {
                        diagnostics.Add("Web Consultant could not complete discovery: " + ex.Message);
                        Debug.WriteLine($"[WEB CONSULTANT INTEGRATION] Failed | Title='{title}' | {ex.Message}");
                    }
                }

                // Deduplicate identical values from different providers while
                // retaining the strongest evidence.
                List<MetadataResearchCandidate> merged =
                    candidates
                        .GroupBy(
                            candidate =>
                                candidate.Field + "|" + Normalize(candidate.Value),
                            StringComparer.OrdinalIgnoreCase)
                        .Select(group =>
                            group.OrderByDescending(candidate => candidate.Confidence)
                                .First())
                        .OrderByDescending(candidate => candidate.Confidence)
                        .ToList();

                Debug.WriteLine(
                    $"[METADATA RESULT TRACE] Title='{title}' | Author='{author}' | " +
                    $"RawCandidates={candidates.Count} | MergedCandidates={merged.Count} | " +
                    $"DescriptionCandidates={merged.Count(c => string.Equals(c.Field, "Description", StringComparison.OrdinalIgnoreCase))} | " +
                    $"PublisherCandidates={merged.Count(c => string.Equals(c.Field, "Publisher", StringComparison.OrdinalIgnoreCase))} | " +
                    $"ProviderResponded={providerResponded} | ProviderSucceeded={providerSucceeded} | " +
                    $"RateLimited={anyRateLimited} | TimedOut={anyTimedOut}");

                foreach (MetadataResearchCandidate traceCandidate in merged)
                {
                    Debug.WriteLine(
                        $"[METADATA CANDIDATE TRACE] Merged | " +
                        $"Title='{title}' | Field={traceCandidate.Field} | " +
                        $"Confidence={traceCandidate.Confidence:P0} | " +
                        $"Value='{traceCandidate.Value}' | Source='{traceCandidate.Source}'");
                }

                bool requestedMetadataStillMissing =
                    (string.IsNullOrWhiteSpace(metadata.Description) &&
                     !HasFullDescriptionCandidate(merged)) ||
                    (string.IsNullOrWhiteSpace(metadata.Publisher) &&
                     !merged.Any(candidate =>
                         string.Equals(
                             candidate.Field,
                             "Publisher",
                             StringComparison.OrdinalIgnoreCase)));

                if (anyRateLimited && requestedMetadataStillMissing)
                {
                    return new MetadataResearchResult
                    {
                        Candidates = merged,
                        RateLimited = true,
                        RetryAfterSeconds = retryAfterSeconds,
                        Diagnostics = diagnostics
                    };
                }

                if (!providerSucceeded && anyTimedOut && !providerResponded)
                {
                    return new MetadataResearchResult
                    {
                        Candidates = merged,
                        TimedOut = true,
                        RetryAfterSeconds = retryAfterSeconds,
                        Diagnostics = diagnostics
                    };
                }

                if (!providerResponded)
                {
                    return new MetadataResearchResult
                    {
                        Candidates = merged,
                        ProviderUnavailable = true,
                        RetryAfterSeconds = retryAfterSeconds,
                        Diagnostics = diagnostics
                    };
                }

                return new MetadataResearchResult
                {
                    Candidates = merged,
                    RetryAfterSeconds = retryAfterSeconds,
                    Diagnostics = diagnostics
                };
            }
            catch (OperationCanceledException)
            {
                return MetadataResearchResult.CreateTimedOut();
            }
            catch (System.Net.Http.HttpRequestException)
            {
                return MetadataResearchResult.CreateProviderUnavailable();
            }
            catch (JsonException)
            {
                return MetadataResearchResult.CreateProviderUnavailable();
            }
            catch
            {
                return MetadataResearchResult.CreateProviderUnavailable();
            }
        }

        private static string BuildOpenLibraryIdentitySearchUrl(
            string title,
            string author)
        {
            List<string> parameters = new();

            if (!string.IsNullOrWhiteSpace(title))
            {
                parameters.Add(
                    "title=" + Uri.EscapeDataString(title.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                parameters.Add(
                    "author=" + Uri.EscapeDataString(author.Trim()));
            }

            parameters.Add(
                "fields=" + Uri.EscapeDataString(
                    "key,title,author_name,isbn,edition_key,publisher,publish_year,description,first_sentence"));
            parameters.Add("limit=10");

            return "https://openlibrary.org/search.json?" +
                   string.Join("&", parameters);
        }

        private static bool HasFullDescriptionCandidate(
            IEnumerable<MetadataResearchCandidate> candidates)
        {
            return candidates.Any(candidate =>
                string.Equals(
                    candidate.Field,
                    "Description",
                    StringComparison.OrdinalIgnoreCase) &&
                !candidate.Details.Any(detail => detail.Contains(
                    "first-sentence summary rather than a full synopsis",
                    StringComparison.OrdinalIgnoreCase)));
        }

        private static string? BuildBestOpenLibraryWorkUrl(
            string? searchJson,
            string requestedTitle,
            string requestedAuthor)
        {
            if (string.IsNullOrWhiteSpace(searchJson))
                return null;

            using JsonDocument document = JsonDocument.Parse(searchJson);
            if (!document.RootElement.TryGetProperty("docs", out JsonElement docs) ||
                docs.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            E_EbookMetadata identity = new()
            {
                Title = requestedTitle,
                Author = requestedAuthor
            };

            string? bestWorkKey = null;
            double bestScore = 0.0;

            foreach (JsonElement doc in docs.EnumerateArray())
            {
                string key = GetString(doc, "key");
                if (!key.StartsWith("/works/", StringComparison.OrdinalIgnoreCase))
                    continue;

                double score = CalculateMatchConfidence(
                    identity,
                    GetString(doc, "title"),
                    GetStringArray(doc, "author_name"),
                    GetStringArray(doc, "isbn"));

                if (score > bestScore)
                {
                    bestScore = score;
                    bestWorkKey = key;
                }
            }

            if (bestScore < 0.75 || string.IsNullOrWhiteSpace(bestWorkKey))
                return null;

            return "https://openlibrary.org" + bestWorkKey + ".json";
        }

        private static MetadataResearchCandidate? ParseOpenLibraryWorkDescription(
            string json,
            E_EbookMetadata metadata,
            string sourceUrl)
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            string title = GetString(root, "title");
            string description = StripSimpleHtml(
                GetDescriptionString(root, "description")).Trim();

            if (string.IsNullOrWhiteSpace(description) ||
                description.Length < 100)
            {
                return null;
            }

            // The work endpoint has a matching work key selected from the
            // title/author search results. Use conservative confidence; the
            // edition can still differ and the decision engine remains in charge.
            return new MetadataResearchCandidate
            {
                Field = "Description",
                Value = description,
                Source = sourceUrl,
                Confidence = 0.88,
                Evidence = BuildEvidence(title, SplitAuthors(metadata.Author), string.Empty),
                Details =
                {
                    "Open Library Work record supplied a fuller description than the search result's first sentence; verify edition relevance."
                }
            };
        }

        private static string BuildInternetArchiveSearchUrl(
            string title,
            string author)
        {
            List<string> terms = new();

            terms.Add("title:\"" + title.Trim().Replace("\"", "") + "\"");

            if (!string.IsNullOrWhiteSpace(author))
            {
                // EPUB author metadata may contain multiple authors separated
                // by semicolons. Search using the first normalized author name
                // rather than a combined string that catalog records rarely use.
                string primaryAuthor = SplitAuthors(author).FirstOrDefault() ?? author;
                terms.Add("creator:\"" + primaryAuthor.Trim().Replace("\"", "") + "\"");
            }

            string query = string.Join(" AND ", terms);
            string[] fields =
            {
                "identifier", "title", "creator", "description",
                "publisher", "isbn", "date", "mediatype"
            };

            List<string> parameters = new()
            {
                "q=" + Uri.EscapeDataString(query)
            };

            parameters.AddRange(fields.Select(field =>
                "fl[]=" + Uri.EscapeDataString(field)));
            parameters.Add("rows=10");
            parameters.Add("page=1");
            parameters.Add("output=json");

            return "https://archive.org/advancedsearch.php?" +
                   string.Join("&", parameters);
        }

        private static string BuildGoogleBooksIdentitySearchUrl(
            string title,
            string author)
        {
            List<string> terms = new();

            if (!string.IsNullOrWhiteSpace(title))
                terms.Add("intitle:" + title.Trim());

            if (!string.IsNullOrWhiteSpace(author))
            {
                foreach (string authorPart in SplitAuthors(author))
                    terms.Add("inauthor:" + authorPart);
            }

            string query = string.Join(" ", terms);

            return
                "https://www.googleapis.com/books/v1/volumes?q=" +
                Uri.EscapeDataString(query) +
                "&maxResults=10&printType=books";
        }

        private static string BuildOpenLibrarySearchUrl(
            string title,
            string author,
            string isbn)
        {
            List<string> parameters = new();

            if (!string.IsNullOrWhiteSpace(isbn))
            {
                parameters.Add(
                    "isbn=" + Uri.EscapeDataString(isbn.Trim()));
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(title))
                    parameters.Add("title=" + Uri.EscapeDataString(title.Trim()));

                if (!string.IsNullOrWhiteSpace(author))
                    parameters.Add("author=" + Uri.EscapeDataString(author.Trim()));
            }

            parameters.Add(
                "fields=" + Uri.EscapeDataString(
                    "key,title,author_name,isbn,edition_key,publisher,publish_year,description,first_sentence"));
            parameters.Add("limit=10");

            return "https://openlibrary.org/search.json?" +
                   string.Join("&", parameters);
        }

        private static string BuildGoogleBooksSearchUrl(
            string title,
            string author,
            string isbn)
        {
            string query;

            if (!string.IsNullOrWhiteSpace(isbn))
            {
                query = "isbn:" + isbn.Trim();
            }
            else
            {
                List<string> terms = new();

                if (!string.IsNullOrWhiteSpace(title))
                    terms.Add("intitle:" + title.Trim());

                if (!string.IsNullOrWhiteSpace(author))
                    terms.Add("inauthor:" + author.Trim());

                query = string.Join(" ", terms);
            }

            return BuildGoogleBooksUrl(query);
        }

        private static string BuildGoogleBooksBroadSearchUrl(
            string title,
            string author)
        {
            List<string> terms = new();

            if (!string.IsNullOrWhiteSpace(title))
                terms.Add(title.Trim());

            if (!string.IsNullOrWhiteSpace(author))
                terms.Add(author.Trim());

            return BuildGoogleBooksUrl(string.Join(" ", terms));
        }

        private static string BuildGoogleBooksUrl(string query)
        {
            return "https://www.googleapis.com/books/v1/volumes?q=" +
                   Uri.EscapeDataString(query) +
                   "&maxResults=10&printType=books";
        }

        private static E_ExternalProviderFetchResult TryFetchJson(
            string url,
            string provider)
        {
            return E_ExternalProviderGateway.FetchJson(
                provider,
                url);
        }

        private static void TraceProviderFetch(
            string stage,
            string url,
            E_ExternalProviderFetchResult result)
        {
            Debug.WriteLine(
                $"[METADATA PROVIDER STATUS] Stage='{stage}' | " +
                $"Status={result.Status} | Responded={result.Responded} | " +
                $"Succeeded={result.Succeeded} | RateLimited={result.RateLimited} | " +
                $"TimedOut={result.TimedOut} | RetryAfterSeconds={result.RetryAfterSeconds} | " +
                $"Url='{url}' | Diagnostic='{result.Diagnostic}'");
        }

        private static void AddDiagnostic(
            List<string> diagnostics,
            string diagnostic)
        {
            if (!string.IsNullOrWhiteSpace(diagnostic))
                diagnostics.Add(diagnostic);
        }

        private static List<MetadataResearchCandidate> ParseOpenLibraryCandidates(
            string json,
            E_EbookMetadata metadata,
            string sourceUrl)
        {
            List<MetadataResearchCandidate> candidates = new();

            using JsonDocument document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty("docs", out JsonElement docs) ||
                docs.ValueKind != JsonValueKind.Array)
            {
                return candidates;
            }

            foreach (JsonElement doc in docs.EnumerateArray())
            {
                string title = GetString(doc, "title");
                List<string> authors = GetStringArray(doc, "author_name");
                List<string> publishers = GetStringArray(doc, "publisher");
                List<string> isbns = GetStringArray(doc, "isbn");
                string publisher = publishers.FirstOrDefault() ?? string.Empty;

                string description =
                    GetDescriptionString(doc, "description");

                if (string.IsNullOrWhiteSpace(description))
                {
                    List<string> sentences = GetStringArray(doc, "first_sentence");
                    description = sentences.FirstOrDefault() ?? string.Empty;
                }

                double match = CalculateMatchConfidence(
                    metadata,
                    title,
                    authors,
                    isbns);

                if (!string.IsNullOrWhiteSpace(publisher))
                {
                    candidates.Add(new MetadataResearchCandidate
                    {
                        Field = "Publisher",
                        Value = publisher.Trim(),
                        Source = sourceUrl,
                        Confidence = Math.Min(match + 0.20, 1.0),
                        Evidence = BuildEvidence(title, authors, publisher),
                        Details =
                        {
                            $"Publisher found by Open Library: {publisher.Trim()}."
                        }
                    });
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    bool isFullDescription =
                        !string.IsNullOrWhiteSpace(
                            GetDescriptionString(doc, "description"));

                    candidates.Add(new MetadataResearchCandidate
                    {
                        Field = "Description",
                        Value = description.Trim(),
                        Source = sourceUrl,
                        Confidence = isFullDescription
                            ? Math.Min(match, 0.95)
                            : Math.Min(match, 0.85),
                        Evidence = BuildEvidence(title, authors, publisher),
                        Details =
                        {
                            isFullDescription
                                ? "Open Library supplied a book description."
                                : "Open Library supplied a first-sentence summary rather than a full synopsis."
                        }
                    });
                }
            }

            return candidates;
        }

        private static List<MetadataResearchCandidate> ParseInternetArchiveCandidates(
            string json,
            E_EbookMetadata metadata,
            string sourceUrl)
        {
            List<MetadataResearchCandidate> candidates = new();

            using JsonDocument document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty("response", out JsonElement response) ||
                !response.TryGetProperty("docs", out JsonElement docs) ||
                docs.ValueKind != JsonValueKind.Array)
            {
                return candidates;
            }

            foreach (JsonElement doc in docs.EnumerateArray())
            {
                string resultTitle = GetStringOrArray(doc, "title");
                List<string> authors = GetStringOrArrayValues(doc, "creator");
                List<string> isbns = GetStringOrArrayValues(doc, "isbn");
                string publisher = GetStringOrArray(doc, "publisher");
                string description = StripSimpleHtml(
                    GetStringOrArray(doc, "description")).Trim();

                // Do not offer weakly identified archive uploads as metadata
                // for a user's book. Title and author evidence must be strong.
                double match = CalculateMatchConfidence(
                    metadata,
                    resultTitle,
                    authors,
                    isbns);

                if (match < 0.65)
                    continue;

                if (!string.IsNullOrWhiteSpace(publisher))
                {
                    candidates.Add(new MetadataResearchCandidate
                    {
                        Field = "Publisher",
                        Value = publisher.Trim(),
                        Source = sourceUrl,
                        Confidence = Math.Min(match + 0.15, 0.95),
                        Evidence = BuildEvidence(resultTitle, authors, publisher),
                        Details =
                        {
                            "Publisher metadata found in an Internet Archive catalog record; verify it matches this edition."
                        }
                    });
                }

                // Deliberately do not offer Internet Archive item descriptions
                // as book synopses. They can describe a scan, upload, or record.
                // This provider remains useful for corroborating publisher data.
            }

            return candidates;
        }

        private static List<MetadataResearchCandidate> ParseGoogleBooksCandidates(
            string json,
            E_EbookMetadata metadata,
            string sourceUrl)
        {
            List<MetadataResearchCandidate> candidates = new();

            using JsonDocument document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty("items", out JsonElement items) ||
                items.ValueKind != JsonValueKind.Array)
            {
                return candidates;
            }

            foreach (JsonElement item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("volumeInfo", out JsonElement info) ||
                    info.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                string title = GetString(info, "title");
                List<string> authors = GetStringArray(info, "authors");
                string publisher = GetString(info, "publisher");
                string description = GetString(info, "description");

                List<string> isbns = GetGoogleIsbns(info);

                double match = CalculateMatchConfidence(
                    metadata,
                    title,
                    authors,
                    isbns);

                if (!string.IsNullOrWhiteSpace(publisher))
                {
                    candidates.Add(new MetadataResearchCandidate
                    {
                        Field = "Publisher",
                        Value = publisher.Trim(),
                        Source = sourceUrl,
                        Confidence = Math.Min(match + 0.20, 1.0),
                        Evidence = BuildEvidence(title, authors, publisher),
                        Details =
                        {
                            $"Publisher found by Google Books: {publisher.Trim()}."
                        }
                    });
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    candidates.Add(new MetadataResearchCandidate
                    {
                        Field = "Description",
                        Value = StripSimpleHtml(description).Trim(),
                        Source = sourceUrl,
                        Confidence = Math.Min(match, 0.95),
                        Evidence = BuildEvidence(title, authors, publisher),
                        Details =
                        {
                            "Google Books supplied a volume description/synopsis."
                        }
                    });
                }
            }

            return candidates;
        }

        private static double CalculateMatchConfidence(
            E_EbookMetadata metadata,
            string resultTitle,
            IReadOnlyList<string> resultAuthors,
            IReadOnlyList<string> resultIsbns)
        {
            double score = 0.0;

            if (!string.IsNullOrWhiteSpace(metadata.Isbn))
            {
                string requested = Normalize(metadata.Isbn);
                if (resultIsbns.Any(isbn => Normalize(isbn) == requested))
                    return 0.95;
            }

            string sourceTitle = Normalize(metadata.Title);
            string foundTitle = Normalize(resultTitle);

            if (!string.IsNullOrWhiteSpace(sourceTitle) &&
                !string.IsNullOrWhiteSpace(foundTitle))
            {
                if (string.Equals(
                        sourceTitle,
                        foundTitle,
                        StringComparison.OrdinalIgnoreCase))
                {
                    score += 0.65;
                }
                else if (foundTitle.Contains(sourceTitle) ||
                         sourceTitle.Contains(foundTitle))
                {
                    score += 0.35;
                }
            }

            // EPUB author metadata often contains several co-authors in one
            // string, while Google Books returns them as separate authors.
            // Comparing the whole normalized strings incorrectly penalized
            // books such as Chrome Circle (Mercedes Lackey + Larry Dixon).
            // Treat an exact normalized author as strong evidence even when
            // other co-authors are present.
            string[] sourceAuthors = SplitAuthors(metadata.Author)
                .Select(Normalize)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            string[] foundAuthors = resultAuthors
                .Select(Normalize)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (sourceAuthors.Length > 0 && foundAuthors.Length > 0)
            {
                int exactMatches =
                    sourceAuthors.Count(source =>
                        foundAuthors.Any(found =>
                            string.Equals(
                                source,
                                found,
                                StringComparison.OrdinalIgnoreCase)));

                if (exactMatches == sourceAuthors.Length)
                    score += 0.30;
                else if (exactMatches > 0)
                    score += 0.25;
                else if (foundAuthors.Any(found =>
                             sourceAuthors.Any(source =>
                                 found.Contains(source) ||
                                 source.Contains(found))))
                    score += 0.15;
            }

            return Math.Min(score, 0.95);
        }

        private static IReadOnlyList<string> SplitAuthors(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<string>();

            return value
                .Split(
                    new[] { ';', '|', '&' },
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .SelectMany(part =>
                    part.Contains(" and ", StringComparison.OrdinalIgnoreCase)
                        ? part.Split(
                            new[] { " and " },
                            StringSplitOptions.RemoveEmptyEntries |
                            StringSplitOptions.TrimEntries)
                        : new[] { part })
                .ToArray();
        }

        private static List<string> GetGoogleIsbns(JsonElement info)
        {
            List<string> values = new();

            if (!info.TryGetProperty("industryIdentifiers", out JsonElement ids) ||
                ids.ValueKind != JsonValueKind.Array)
            {
                return values;
            }

            foreach (JsonElement id in ids.EnumerateArray())
            {
                string value = GetString(id, "identifier");
                if (!string.IsNullOrWhiteSpace(value))
                    values.Add(value);
            }

            return values;
        }

        private static string GetDescriptionString(
            JsonElement element,
            string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement value))
                return string.Empty;

            if (value.ValueKind == JsonValueKind.String)
                return value.GetString() ?? string.Empty;

            // Open Library may expose richer description objects. Prefer their
            // value/text member when present.
            if (value.ValueKind == JsonValueKind.Object)
            {
                string text = GetString(value, "value");
                if (!string.IsNullOrWhiteSpace(text))
                    return text;

                text = GetString(value, "text");
                if (!string.IsNullOrWhiteSpace(text))
                    return text;
            }

            return string.Empty;
        }

        private static string StripSimpleHtml(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string withoutTags =
                System.Text.RegularExpressions.Regex.Replace(
                    value,
                    "<[^>]+>",
                    " ");

            return WebUtility.HtmlDecode(withoutTags)
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
        }

        private static string GetValue(
            string metadataValue,
            IReadOnlyList<MetadataEvidence> evidence,
            string field)
        {
            if (!string.IsNullOrWhiteSpace(metadataValue))
                return metadataValue.Trim();

            return evidence.FirstOrDefault(item =>
                string.Equals(item.Field, field, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(item.Value))?.Value.Trim() ?? string.Empty;
        }

        private static string GetString(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out JsonElement value) &&
                   value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }

        private static string GetStringOrArray(
            JsonElement element,
            string property)
        {
            List<string> values = GetStringOrArrayValues(element, property);
            return string.Join("; ", values);
        }

        private static List<string> GetStringOrArrayValues(
            JsonElement element,
            string property)
        {
            List<string> values = new();

            if (!element.TryGetProperty(property, out JsonElement value))
                return values;

            if (value.ValueKind == JsonValueKind.String)
            {
                string text = value.GetString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(text))
                    values.Add(text.Trim());
            }
            else if (value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in value.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.String)
                        continue;

                    string text = item.GetString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(text))
                        values.Add(text.Trim());
                }
            }

            return values;
        }

        private static List<string> GetStringArray(JsonElement element, string property)
        {
            List<string> values = new();

            if (!element.TryGetProperty(property, out JsonElement array) ||
                array.ValueKind != JsonValueKind.Array)
                return values;

            foreach (JsonElement item in array.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    string value = item.GetString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(value))
                        values.Add(value.Trim());
                }
            }

            return values;
        }

        private static string BuildEvidence(
            string title,
            IReadOnlyList<string> authors,
            string publisher)
        {
            string authorText = authors.Count == 0
                ? "unknown author"
                : string.Join(", ", authors);

            return
                $"Book metadata match: \"{title}\" by {authorText}. " +
                $"Publisher: {publisher}.";
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            char[] chars = value
                .Trim()
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray();

            return new string(chars);
        }
    }

    internal sealed class MetadataResearchResult
    {
        public List<MetadataResearchCandidate> Candidates { get; init; } = new();

        public bool TimedOut { get; init; }

        public bool ProviderUnavailable { get; init; }

        public bool RateLimited { get; init; }

        public int RetryAfterSeconds { get; init; }

        public List<string> Diagnostics { get; init; } = new();

        public static MetadataResearchResult CreateTimedOut() =>
            new() { TimedOut = true };

        public static MetadataResearchResult CreateProviderUnavailable() =>
            new() { ProviderUnavailable = true };
    }

    internal sealed class MetadataResearchCandidate
    {
        public string Field { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
        public string Source { get; init; } = string.Empty;
        public string Evidence { get; init; } = string.Empty;
        public double Confidence { get; init; }
        public List<string> Details { get; } = new();
    }
}
