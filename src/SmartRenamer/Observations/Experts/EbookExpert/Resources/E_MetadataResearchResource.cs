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
            string isbn = GetValue(metadata.Isbn, evidence, "ISBN");

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

                if (E_ExternalProviderGateway.TryGetCachedJson(
                        identityOpenLibraryUrl,
                        out string? cachedOpenLibraryJson) &&
                    !string.IsNullOrWhiteSpace(cachedOpenLibraryJson))
                {
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

                    openLibraryEvidenceSatisfiesRequest =
                        (!string.IsNullOrWhiteSpace(metadata.Publisher) ||
                         candidates.Any(candidate =>
                             string.Equals(
                                 candidate.Field,
                                 "Publisher",
                                 StringComparison.OrdinalIgnoreCase))) &&
                        (!string.IsNullOrWhiteSpace(metadata.Description) ||
                         candidates.Any(candidate =>
                             string.Equals(
                                 candidate.Field,
                                 "Description",
                                 StringComparison.OrdinalIgnoreCase)));

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
                        List<MetadataResearchCandidate> fetchedOpenLibraryCandidates =
                            ParseOpenLibraryCandidates(
                                openLibrary.Json!,
                                metadata,
                                openLibraryUrl);

                        candidates.AddRange(fetchedOpenLibraryCandidates);

                        Debug.WriteLine(
                            $"[METADATA CANDIDATE TRACE] Open Library | " +
                            $"Title='{title}' | Candidates={fetchedOpenLibraryCandidates.Count}");
                    }
                }

                // Google Books is a second, independent provider. It is used
                // only when the requested metadata is still missing. A Google
                // rate limit therefore does not cause Scout to hammer Google
                // again; the gateway cools Google independently.
                bool needGoogle =
                    string.IsNullOrWhiteSpace(metadata.Description) ||
                    !candidates.Any(candidate =>
                        string.Equals(
                            candidate.Field,
                            "Publisher",
                            StringComparison.OrdinalIgnoreCase));

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
                             candidates.Any(candidate =>
                                 string.Equals(
                                     candidate.Field,
                                     "Description",
                                     StringComparison.OrdinalIgnoreCase))) &&
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
                    }

                    // Do not immediately issue a second Google request just
                    // because the first search produced no description. That
                    // pattern was one of the request multipliers that could
                    // turn a 10-book expedition into a much larger provider
                    // burst. A later expedition/research attempt can use the
                    // cached/cooldown-aware provider again.
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
                     !merged.Any(candidate =>
                         string.Equals(
                             candidate.Field,
                             "Description",
                             StringComparison.OrdinalIgnoreCase))) ||
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
