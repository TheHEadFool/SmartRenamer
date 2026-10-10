using Scout.Observations.Experts.EbookExpert.Data;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;

namespace SmartRenamer.Observations.Experts.EbookExpert.Resources
{
    /// <summary>
    /// Researches ISBN candidates without deciding or applying a repair.
    ///
    /// Research policy:
    ///   1. Receive a locally reconciled Title/Author research identity.
    ///   2. Try Open Library first.
    ///   3. If Open Library produces no usable ISBNs, use its broader identity
    ///      and edition recovery paths before another provider.
    ///   4. Use Google Books only as a fallback when the primary source has not
    ///      established a strong candidate.
    ///   5. Merge corroborating ISBNs from independent providers.
    ///
    /// A provider failure is never converted into "no ISBN exists".
    /// </summary>
    internal sealed class E_IsbnResearchResource
    {
        private static readonly HttpClient HttpClient = CreateHttpClient();

        public List<IsbnResearchCandidate> Research(
            E_EbookMetadata metadata,
            string? userEvidence,
            IReadOnlyList<MetadataEvidence> evidence)
        {
            return ResearchWithStatus(metadata, userEvidence, evidence).Candidates;
        }

        public IsbnResearchResult ResearchWithStatus(
            E_EbookMetadata metadata,
            string? userEvidence,
            IReadOnlyList<MetadataEvidence> evidence)
        {
            ArgumentNullException.ThrowIfNull(metadata);
            ArgumentNullException.ThrowIfNull(evidence);

            string searchTitle = GetEvidenceBackedValue(metadata.Title, evidence, "Title");
            string searchAuthor = GetEvidenceBackedValue(metadata.Author, evidence, "Author");
            string searchPublisher = GetEvidenceBackedValue(metadata.Publisher, evidence, "Publisher");

            if (string.IsNullOrWhiteSpace(searchTitle) &&
                string.IsNullOrWhiteSpace(searchAuthor))
            {
                return new IsbnResearchResult
                {
                    Status = IsbnResearchStatus.NoCandidates,
                    Diagnostics = { "Scout did not have enough title/author identity information to perform ISBN research." }
                };
            }

            List<IsbnResearchCandidate> allCandidates = new();
            List<string> diagnostics = new();
            bool anyProviderSucceeded = false;
            bool anyTimedOut = false;
            bool anyRateLimited = false;
            int retryAfterSeconds = 0;

            //-------------------------------------------------------------
            // Open Library is Scout's primary ISBN source.
            //
            // The previous order asked Google first. That made a Google 429
            // visible even when Open Library could have answered the request.
            // Open Library also has a second, broader search form that is
            // useful when the structured title+author search returns zero
            // documents. We use that fallback before asking another provider.
            //-------------------------------------------------------------
            List<IsbnResearchCandidate> openCandidates =
                ResearchOpenLibrary(
                    searchTitle,
                    searchAuthor,
                    metadata.Series,
                    userEvidence,
                    metadata,
                    searchPublisher,
                    diagnostics,
                    ref anyProviderSucceeded,
                    ref anyTimedOut,
                    ref anyRateLimited,
                    ref retryAfterSeconds);

            allCandidates.AddRange(openCandidates);

            //-------------------------------------------------------------
            // Only use Google Books when Open Library did not establish a
            // strong candidate. This keeps provider fallback useful without
            // turning a temporary Google 429 into the visible explanation for
            // an ISBN that Open Library can establish.
            //-------------------------------------------------------------
            bool needGoogle =
                allCandidates.Count == 0 ||
                allCandidates.Max(candidate => candidate.Confidence) < 0.90;

            if (needGoogle)
            {
                string googleUrl =
                    BuildGoogleBooksSearchUrl(
                        searchTitle,
                        searchAuthor,
                        userEvidence);

                ProviderFetchResult google =
                    FetchJson(googleUrl, "Google Books");

                diagnostics.Add(google.Diagnostic);
                anyProviderSucceeded |= google.Succeeded;
                anyTimedOut |= google.TimedOut;
                anyRateLimited |= google.RateLimited;
                retryAfterSeconds = Math.Max(
                    retryAfterSeconds,
                    google.RetryAfterSeconds);

                if (google.Responded && !string.IsNullOrWhiteSpace(google.Json))
                {
                    try
                    {
                        List<IsbnResearchCandidate> googleCandidates =
                            ParseGoogleBooksCandidates(
                                google.Json,
                                metadata,
                                searchPublisher,
                                googleUrl);

                        allCandidates.AddRange(googleCandidates);
                        diagnostics.Add(
                            $"Google Books returned {googleCandidates.Count} usable ISBN candidate(s).");
                    }
                    catch (JsonException)
                    {
                        diagnostics.Add(
                            "Google Books returned data Scout could not parse as book metadata.");
                    }
                }
            }

            List<IsbnResearchCandidate> merged =
                MergeCandidates(allCandidates);

            Debug.WriteLine(
                "Scout ISBN research: " +
                string.Join(" | ", diagnostics));

            if (merged.Count > 0)
            {
                return new IsbnResearchResult
                {
                    Status = IsbnResearchStatus.Succeeded,
                    Candidates = merged,
                    RetryAfterSeconds = retryAfterSeconds,
                    Diagnostics = diagnostics
                };
            }

            return new IsbnResearchResult
            {
                Status = anyRateLimited && merged.Count == 0
                    ? IsbnResearchStatus.RateLimited
                    : !anyProviderSucceeded && anyTimedOut
                        ? IsbnResearchStatus.TimedOut
                        : !anyProviderSucceeded
                            ? IsbnResearchStatus.ProviderUnavailable
                            : IsbnResearchStatus.NoCandidates,
                RetryAfterSeconds = retryAfterSeconds,
                Diagnostics = diagnostics
            };
        }

        private static ProviderFetchResult FetchJson(
            string url,
            string provider)
        {
            E_ExternalProviderFetchResult fetch =
                E_ExternalProviderGateway.FetchJson(
                    provider,
                    url);

            return new ProviderFetchResult
            {
                Responded = fetch.Responded,
                Succeeded = fetch.Succeeded,
                TimedOut = fetch.TimedOut,
                RateLimited = fetch.RateLimited,
                Json = fetch.Json,
                RetryAfterSeconds = fetch.RetryAfterSeconds,
                Diagnostic = fetch.Diagnostic
            };
        }

        private static List<IsbnResearchCandidate> ResearchOpenLibrary(
            string title,
            string author,
            string series,
            string? userEvidence,
            E_EbookMetadata metadata,
            string knownPublisher,
            List<string> diagnostics,
            ref bool anyProviderSucceeded,
            ref bool anyTimedOut,
            ref bool anyRateLimited,
            ref int retryAfterSeconds)
        {
            List<IsbnResearchCandidate> allCandidates = new();

            string primaryUrl =
                BuildOpenLibrarySearchUrl(
                    title,
                    author,
                    userEvidence);

            ProviderFetchResult primary =
                FetchJson(primaryUrl, "Open Library");

            diagnostics.Add(primary.Diagnostic);
            anyProviderSucceeded |= primary.Succeeded;
            anyTimedOut |= primary.TimedOut;
            anyRateLimited |= primary.RateLimited;
            retryAfterSeconds = Math.Max(
                retryAfterSeconds,
                primary.RetryAfterSeconds);

            bool primaryReturnedDocuments = false;

            if (primary.Responded && !string.IsNullOrWhiteSpace(primary.Json))
            {
                primaryReturnedDocuments =
                    CountOpenLibraryDocuments(primary.Json) > 0;

                List<IsbnResearchCandidate> candidates =
                    ParseOpenLibraryCandidates(
                        primary.Json,
                        metadata,
                        knownPublisher,
                        primaryUrl);

                allCandidates.AddRange(candidates);
                diagnostics.Add(
                    $"Open Library structured search returned {candidates.Count} usable ISBN candidate(s).");

                if (candidates.Count == 0)
                {
                    List<IsbnResearchCandidate> editionCandidates =
                        RecoverIsbnsFromTopOpenLibraryEditions(
                            primary.Json,
                            metadata,
                            knownPublisher);

                    allCandidates.AddRange(editionCandidates);

                    if (editionCandidates.Count > 0)
                    {
                        diagnostics.Add(
                            $"Open Library edition records supplied {editionCandidates.Count} additional ISBN candidate(s).");
                    }
                }
            }

            //-------------------------------------------------------------
            // IMPORTANT: zero documents is different from "the book has no
            // ISBN". Open Library's structured title/author parameters can
            // be overly restrictive for records with alternate authors,
            // subtitles, series labels, or catalog normalization differences.
            //
            // Give Open Library one broader identity search before falling
            // back to another provider. This is the missing recovery path
            // exposed by Chrome Circle: the known book exists externally, but
            // the first query returned zero documents.
            //-------------------------------------------------------------
            if (allCandidates.Count == 0 &&
                (primaryReturnedDocuments || primary.Succeeded))
            {
                string fallbackIdentity = BuildOpenLibraryIdentityQuery(
                    title,
                    author,
                    series,
                    userEvidence);

                if (!string.IsNullOrWhiteSpace(fallbackIdentity))
                {
                    string fallbackUrl =
                        BuildOpenLibraryQueryUrl(fallbackIdentity);

                    ProviderFetchResult fallback =
                        FetchJson(fallbackUrl, "Open Library");

                    diagnostics.Add(
                        "Open Library used a broader identity query after the structured search produced no usable candidates.");
                    diagnostics.Add(fallback.Diagnostic);
                    anyProviderSucceeded |= fallback.Succeeded;
                    anyTimedOut |= fallback.TimedOut;
                    anyRateLimited |= fallback.RateLimited;
                    retryAfterSeconds = Math.Max(
                        retryAfterSeconds,
                        fallback.RetryAfterSeconds);

                    if (fallback.Responded &&
                        !string.IsNullOrWhiteSpace(fallback.Json))
                    {
                        List<IsbnResearchCandidate> fallbackCandidates =
                            ParseOpenLibraryCandidates(
                                fallback.Json,
                                metadata,
                                knownPublisher,
                                fallbackUrl);

                        allCandidates.AddRange(fallbackCandidates);
                        diagnostics.Add(
                            $"Open Library broad identity search returned {fallbackCandidates.Count} usable ISBN candidate(s).");

                        if (fallbackCandidates.Count == 0)
                        {
                            List<IsbnResearchCandidate> editionCandidates =
                                RecoverIsbnsFromTopOpenLibraryEditions(
                                    fallback.Json,
                                    metadata,
                                    knownPublisher);

                            allCandidates.AddRange(editionCandidates);

                            if (editionCandidates.Count > 0)
                            {
                                diagnostics.Add(
                                    $"Open Library broad-search edition records supplied {editionCandidates.Count} additional ISBN candidate(s).");
                            }
                        }
                    }
                }
            }

            return allCandidates;
        }

        private static int CountOpenLibraryDocuments(string json)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(json);

                return document.RootElement.TryGetProperty("docs", out JsonElement docs) &&
                       docs.ValueKind == JsonValueKind.Array
                    ? docs.GetArrayLength()
                    : 0;
            }
            catch (JsonException)
            {
                return 0;
            }
        }

        private static string BuildOpenLibraryIdentityQuery(
            string title,
            string author,
            string series,
            string? userEvidence)
        {
            List<string> terms = new();

            if (!string.IsNullOrWhiteSpace(title))
                terms.Add(title.Trim());

            if (!string.IsNullOrWhiteSpace(author))
                terms.Add(author.Trim());

            if (!string.IsNullOrWhiteSpace(series))
                terms.Add(series.Trim());

            if (!string.IsNullOrWhiteSpace(userEvidence))
                terms.Add(userEvidence.Trim());

            return string.Join(" ", terms.Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static string BuildOpenLibraryQueryUrl(string query)
        {
            return
                "https://openlibrary.org/search.json?q=" +
                Uri.EscapeDataString(query) +
                "&fields=" + Uri.EscapeDataString(
                    "key,title,author_name,isbn,edition_key,publisher,publish_year,description,first_sentence") +
                "&limit=10";
        }

        private static string BuildGoogleBooksSearchUrl(
            string title,
            string author,
            string? userEvidence)
        {
            List<string> terms = new();

            if (!string.IsNullOrWhiteSpace(title))
                terms.Add("intitle:" + title.Trim());

            if (!string.IsNullOrWhiteSpace(author))
            {
                foreach (string authorPart in SplitAuthors(author))
                    terms.Add("inauthor:" + authorPart);
            }

            if (!string.IsNullOrWhiteSpace(userEvidence))
                terms.Add(userEvidence.Trim());

            string query = string.Join(" ", terms);

            return
                "https://www.googleapis.com/books/v1/volumes?q=" +
                Uri.EscapeDataString(query) +
                "&maxResults=10&printType=books";
        }

        private static string BuildOpenLibrarySearchUrl(
            string title,
            string author,
            string? userEvidence)
        {
            List<string> parameters = new();

            if (!string.IsNullOrWhiteSpace(title))
                parameters.Add("title=" + Uri.EscapeDataString(title.Trim()));

            if (!string.IsNullOrWhiteSpace(author))
                parameters.Add("author=" + Uri.EscapeDataString(author.Trim()));

            if (!string.IsNullOrWhiteSpace(userEvidence))
                parameters.Add("q=" + Uri.EscapeDataString(userEvidence.Trim()));

            parameters.Add(
                "fields=" + Uri.EscapeDataString(
                    "key,title,author_name,isbn,edition_key,publisher,publish_year,description,first_sentence"));
            parameters.Add("limit=10");

            return
                "https://openlibrary.org/search.json?" +
                string.Join("&", parameters);
        }

        private static List<IsbnResearchCandidate> ParseGoogleBooksCandidates(
            string json,
            E_EbookMetadata metadata,
            string knownPublisher,
            string sourceUrl)
        {
            List<IsbnResearchCandidate> candidates = new();

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
                string publicationDate = GetString(info, "publishedDate");
                List<string> isbns = GetGoogleIsbns(info);

                double confidence = CalculateConfidence(
                    metadata,
                    title,
                    authors,
                    publisher,
                    knownPublisher);

                if (confidence < 0.50)
                    continue;

                foreach (string isbnValue in isbns)
                {
                    string isbn = NormalizeIsbn(isbnValue);

                    if (!IsValidIsbn(isbn))
                        continue;

                    candidates.Add(
                        new IsbnResearchCandidate
                        {
                            Isbn = isbn,
                            Title = title,
                            Author = string.Join(", ", authors),
                            Publisher = publisher,
                            PublicationYear = ExtractPublicationYear(publicationDate),
                            Source = sourceUrl,
                            Evidence =
                                BuildEvidence(
                                    "Google Books",
                                    title,
                                    authors,
                                    publisher,
                                    publicationDate,
                                    confidence),
                            EditionVerified = true,
                            VerifiedEditionTitle = title,
                            VerifiedEditionPublisher = publisher,
                            VerifiedEditionPublicationDate = publicationDate,
                            Confidence = confidence
                        });
                }
            }

            return candidates;
        }

        private static List<IsbnResearchCandidate> ParseOpenLibraryCandidates(
            string json,
            E_EbookMetadata metadata,
            string knownPublisher,
            string sourceUrl)
        {
            List<IsbnResearchCandidate> candidates = new();

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
                string publisher = GetStringArray(doc, "publisher").FirstOrDefault() ?? string.Empty;
                string publicationYear =
                    GetStringArrayOrNumbers(doc, "publish_year")
                        .FirstOrDefault() ?? string.Empty;
                List<string> editionKeys = GetStringArray(doc, "edition_key");
                string editionKey = editionKeys.FirstOrDefault() ?? string.Empty;
                List<string> isbns = GetStringArray(doc, "isbn");

                double confidence = CalculateConfidence(
                    metadata,
                    title,
                    authors,
                    publisher,
                    knownPublisher);

                if (confidence < 0.50)
                    continue;

                foreach (string isbnValue in isbns)
                {
                    string isbn = NormalizeIsbn(isbnValue);

                    if (!IsValidIsbn(isbn))
                        continue;

                    candidates.Add(
                        new IsbnResearchCandidate
                        {
                            Isbn = isbn,
                            EditionKey = editionKey,
                            Title = title,
                            Author = string.Join(", ", authors),
                            Publisher = publisher,
                            PublicationYear = publicationYear,
                            Source = sourceUrl,
                            Evidence =
                                BuildEvidence(
                                    "Open Library",
                                    title,
                                    authors,
                                    publisher,
                                    publicationYear,
                                    confidence),
                            EditionVerified = false,
                            Confidence = confidence
                        });
                }
            }

            //-------------------------------------------------------------
            // The Search API already supplies ISBNs. The previous code then
            // performed an ISBN-specific HTTP lookup for every ISBN returned
            // by every matching document. That multiplied a single search
            // into a large burst against Open Library.
            //
            // Verify only the two strongest distinct ISBN candidates. The
            // remaining candidates still retain the Search API evidence and
            // can be evaluated normally without generating more provider
            // traffic.
            //-------------------------------------------------------------
            List<IsbnResearchCandidate> strongest =
                candidates
                    .GroupBy(
                        candidate => candidate.Isbn,
                        StringComparer.OrdinalIgnoreCase)
                    .Select(group =>
                        group.OrderByDescending(candidate => candidate.Confidence)
                            .First())
                    .OrderByDescending(candidate => candidate.Confidence)
                    .Take(2)
                    .ToList();

            foreach (IsbnResearchCandidate candidate in strongest)
            {
                IsbnEditionVerification verification =
                    VerifyIsbnEdition(candidate.Isbn, metadata);

                double verifiedConfidence =
                    CalculateVerifiedConfidence(
                        metadata,
                        candidate.Confidence,
                        candidate.PublicationYear,
                        verification);

                int index = candidates.FindIndex(item =>
                    string.Equals(
                        item.Isbn,
                        candidate.Isbn,
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        item.Source,
                        candidate.Source,
                        StringComparison.OrdinalIgnoreCase));

                if (index < 0)
                    continue;

                IsbnResearchCandidate current = candidates[index];

                candidates[index] =
                    new IsbnResearchCandidate
                    {
                        Isbn = current.Isbn,
                        EditionKey = verification.Found
                            ? verification.EditionKey
                            : current.EditionKey,
                        Title = verification.Found &&
                                !string.IsNullOrWhiteSpace(verification.Title)
                            ? verification.Title
                            : current.Title,
                        Author = current.Author,
                        Publisher = verification.Found &&
                                    !string.IsNullOrWhiteSpace(verification.Publisher)
                            ? verification.Publisher
                            : current.Publisher,
                        PublicationYear = verification.Found &&
                                          !string.IsNullOrWhiteSpace(verification.PublicationDate)
                            ? verification.PublicationDate
                            : current.PublicationYear,
                        Source = current.Source,
                        Evidence = current.Evidence +
                            " " + BuildEditionEvidence(verification),
                        EditionVerified = verification.Found,
                        VerifiedEditionTitle = verification.Title,
                        VerifiedEditionPublisher = verification.Publisher,
                        VerifiedEditionPublicationDate = verification.PublicationDate,
                        VerifiedEditionKey = verification.EditionKey,
                        Confidence = verifiedConfidence
                    };
            }

            return candidates;
        }

        private static List<IsbnResearchCandidate> RecoverIsbnsFromTopOpenLibraryEditions(
            string json,
            E_EbookMetadata metadata,
            string knownPublisher)
        {
            List<IsbnResearchCandidate> candidates = new();

            using JsonDocument document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty("docs", out JsonElement docs) ||
                docs.ValueKind != JsonValueKind.Array)
            {
                return candidates;
            }

            int checkedEditions = 0;

            foreach (JsonElement doc in docs.EnumerateArray())
            {
                if (checkedEditions >= 2)
                    break;

                string title = GetString(doc, "title");
                List<string> authors = GetStringArray(doc, "author_name");
                string publisher = GetStringArray(doc, "publisher").FirstOrDefault() ?? string.Empty;
                string publicationYear =
                    GetStringArrayOrNumbers(doc, "publish_year")
                        .FirstOrDefault() ?? string.Empty;
                string editionKey =
                    GetStringArray(doc, "edition_key")
                        .FirstOrDefault() ?? string.Empty;

                double confidence = CalculateConfidence(
                    metadata,
                    title,
                    authors,
                    publisher,
                    knownPublisher);

                if (confidence < 0.50 || string.IsNullOrWhiteSpace(editionKey))
                    continue;

                checkedEditions++;

                IsbnEditionRecord? edition =
                    FetchOpenLibraryEdition(editionKey);

                if (edition == null)
                    continue;

                foreach (string isbn in edition.Isbns)
                {
                    candidates.Add(
                        new IsbnResearchCandidate
                        {
                            Isbn = isbn,
                            EditionKey = editionKey,
                            Title = string.IsNullOrWhiteSpace(edition.Title)
                                ? title
                                : edition.Title,
                            Author = string.Join(", ", authors),
                            Publisher = string.IsNullOrWhiteSpace(edition.Publisher)
                                ? publisher
                                : edition.Publisher,
                            PublicationYear = string.IsNullOrWhiteSpace(edition.PublicationDate)
                                ? publicationYear
                                : edition.PublicationDate,
                            EditionVerified = true,
                            VerifiedEditionTitle = edition.Title,
                            VerifiedEditionPublisher = edition.Publisher,
                            VerifiedEditionPublicationDate = edition.PublicationDate,
                            VerifiedEditionKey = editionKey,
                            Source = "https://openlibrary.org/books/" + editionKey + ".json",
                            Evidence =
                                $"Open Library edition record verified ISBN {isbn} for " +
                                $"'{edition.Title}'. Publisher: {edition.Publisher}.",
                            Confidence = CalculateVerifiedConfidence(
                                metadata,
                                confidence,
                                publicationYear,
                                new IsbnEditionVerification
                                {
                                    Found = true,
                                    Isbn = isbn,
                                    Title = edition.Title,
                                    Publisher = edition.Publisher,
                                    PublicationDate = edition.PublicationDate,
                                    EditionKey = editionKey
                                })
                        });
                }
            }

            return candidates;
        }

        private static IsbnEditionRecord? FetchOpenLibraryEdition(string editionKey)
        {
            if (string.IsNullOrWhiteSpace(editionKey))
                return null;

            string key = editionKey.Trim();
            if (key.StartsWith("/books/", StringComparison.OrdinalIgnoreCase))
                key = key.Substring("/books/".Length);

            string url =
                "https://openlibrary.org/books/" +
                Uri.EscapeDataString(key) +
                ".json";

            ProviderFetchResult fetch = FetchJson(url, "Open Library edition");

            if (string.IsNullOrWhiteSpace(fetch.Json))
                return null;

            try
            {
                using JsonDocument document = JsonDocument.Parse(fetch.Json);
                JsonElement root = document.RootElement;

                List<string> isbns = new();
                AddJsonArrayValues(root, "isbn_10", isbns);
                AddJsonArrayValues(root, "isbn_13", isbns);

                isbns = isbns
                    .Select(NormalizeIsbn)
                    .Where(IsValidIsbn)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (isbns.Count == 0)
                    return null;

                return new IsbnEditionRecord
                {
                    Title = GetString(root, "title"),
                    Publisher = GetStringArray(root, "publishers").FirstOrDefault() ?? string.Empty,
                    PublicationDate = GetString(root, "publish_date"),
                    Isbns = isbns
                };
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static List<IsbnResearchCandidate> MergeCandidates(
            IEnumerable<IsbnResearchCandidate> candidates)
        {
            return candidates
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Isbn))
                .GroupBy(
                    candidate => NormalizeIsbn(candidate.Isbn),
                    StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    IsbnResearchCandidate strongest =
                        group.OrderByDescending(candidate => candidate.Confidence)
                            .First();

                    int providerCount = group
                        .Select(candidate => GetProviderName(candidate.Source))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count();

                    double confidence = strongest.Confidence;

                    if (providerCount > 1)
                        confidence = Math.Min(confidence + 0.15, 1.0);

                    bool verified = group.Any(candidate => candidate.EditionVerified);

                    List<string> evidence = group
                        .Select(candidate => candidate.Evidence)
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    return new IsbnResearchCandidate
                    {
                        Isbn = strongest.Isbn,
                        EditionKey = strongest.EditionKey,
                        Title = strongest.Title,
                        Author = strongest.Author,
                        Publisher = strongest.Publisher,
                        PublicationYear = strongest.PublicationYear,
                        EditionVerified = verified,
                        VerifiedEditionTitle = strongest.VerifiedEditionTitle,
                        VerifiedEditionPublisher = strongest.VerifiedEditionPublisher,
                        VerifiedEditionPublicationDate = strongest.VerifiedEditionPublicationDate,
                        VerifiedEditionKey = strongest.VerifiedEditionKey,
                        Source = providerCount > 1
                            ? string.Join(" + ", group.Select(c => GetProviderName(c.Source)).Distinct(StringComparer.OrdinalIgnoreCase))
                            : strongest.Source,
                        Evidence = providerCount > 1
                            ? string.Join(" ", evidence) + " Independent provider corroboration increased confidence."
                            : strongest.Evidence,
                        Confidence = confidence
                    };
                })
                .OrderByDescending(candidate => candidate.Confidence)
                .ToList();
        }

        private static double CalculateConfidence(
            E_EbookMetadata metadata,
            string resultTitle,
            IReadOnlyList<string> resultAuthors,
            string resultPublisher,
            string knownPublisher)
        {
            double score = 0.0;

            string sourceTitle = NormalizeText(metadata.Title);
            string foundTitle = NormalizeText(resultTitle);

            if (!string.IsNullOrWhiteSpace(sourceTitle) &&
                !string.IsNullOrWhiteSpace(foundTitle))
            {
                if (string.Equals(sourceTitle, foundTitle, StringComparison.OrdinalIgnoreCase))
                    score += 0.55;
                else if (foundTitle.Contains(sourceTitle) || sourceTitle.Contains(foundTitle))
                    score += 0.30;
            }

            string[] sourceAuthors =
                SplitAuthors(metadata.Author)
                    .Select(NormalizeText)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            string[] foundAuthors =
                resultAuthors
                    .Select(NormalizeText)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            if (sourceAuthors.Length > 0 && foundAuthors.Length > 0)
            {
                int exactMatches =
                    sourceAuthors.Count(source =>
                        foundAuthors.Any(found =>
                            string.Equals(source, found, StringComparison.OrdinalIgnoreCase)));

                if (exactMatches == sourceAuthors.Length)
                    score += 0.35;
                else if (exactMatches > 0)
                    score += 0.25;
                else if (foundAuthors.Any(found =>
                             sourceAuthors.Any(source =>
                                 found.Contains(source) || source.Contains(found))))
                    score += 0.15;
            }

            string sourcePublisher = NormalizeText(metadata.Publisher);
            string foundPublisher = NormalizeText(resultPublisher);
            string evidencePublisher = NormalizeText(knownPublisher);

            if (!string.IsNullOrWhiteSpace(sourcePublisher) &&
                !string.IsNullOrWhiteSpace(foundPublisher))
            {
                if (string.Equals(sourcePublisher, foundPublisher, StringComparison.OrdinalIgnoreCase))
                    score += 0.10;
                else if (foundPublisher.Contains(sourcePublisher) || sourcePublisher.Contains(foundPublisher))
                    score += 0.05;
            }
            else if (!string.IsNullOrWhiteSpace(evidencePublisher) &&
                     !string.IsNullOrWhiteSpace(foundPublisher) &&
                     string.Equals(evidencePublisher, foundPublisher, StringComparison.OrdinalIgnoreCase))
            {
                score += 0.10;
            }

            return Math.Min(score, 1.0);
        }

        private static double CalculateVerifiedConfidence(
            E_EbookMetadata metadata,
            double searchConfidence,
            string searchPublicationYear,
            IsbnEditionVerification verification)
        {
            if (!verification.Found)
                return searchConfidence;

            double score = searchConfidence;

            string sourceTitle = NormalizeText(metadata.Title);
            string verifiedTitle = NormalizeText(verification.Title);

            if (!string.IsNullOrWhiteSpace(sourceTitle) &&
                !string.IsNullOrWhiteSpace(verifiedTitle))
            {
                if (string.Equals(sourceTitle, verifiedTitle, StringComparison.OrdinalIgnoreCase))
                    score += 0.10;
                else if (verifiedTitle.Contains(sourceTitle) || sourceTitle.Contains(verifiedTitle))
                    score += 0.05;
            }

            string sourcePublisher = NormalizeText(metadata.Publisher);
            string verifiedPublisher = NormalizeText(verification.Publisher);

            if (!string.IsNullOrWhiteSpace(sourcePublisher) &&
                !string.IsNullOrWhiteSpace(verifiedPublisher) &&
                string.Equals(sourcePublisher, verifiedPublisher, StringComparison.OrdinalIgnoreCase))
            {
                score += 0.05;
            }

            string searchYear = ExtractPublicationYear(searchPublicationYear);
            string verifiedYear = ExtractPublicationYear(verification.PublicationDate);

            if (!string.IsNullOrWhiteSpace(searchYear) &&
                !string.IsNullOrWhiteSpace(verifiedYear))
            {
                if (string.Equals(searchYear, verifiedYear, StringComparison.OrdinalIgnoreCase))
                    score += 0.05;
                else
                    score -= 0.10;
            }

            return Math.Max(0.0, Math.Min(score, 1.0));
        }

        private static string BuildEvidence(
            string provider,
            string title,
            IReadOnlyList<string> authors,
            string publisher,
            string publicationDate,
            double confidence)
        {
            string authorText = authors.Count > 0
                ? string.Join(", ", authors)
                : "unknown author";

            return
                $"{provider} match: \"{title}\" by {authorText}. " +
                $"Publisher: {publisher}. Publication date: {publicationDate}. " +
                $"Match confidence: {confidence:0.00}.";
        }

        private static string BuildEditionEvidence(
            IsbnEditionVerification verification)
        {
            if (!verification.Found)
                return "ISBN-specific edition verification was not available.";

            return
                $"ISBN-specific verification: {verification.Isbn}. " +
                $"Edition: \"{verification.Title}\". " +
                $"Publisher: {verification.Publisher}. " +
                $"Publication date: {verification.PublicationDate}. " +
                $"Edition key: {verification.EditionKey}.";
        }

        private static string GetEvidenceBackedValue(
            string metadataValue,
            IReadOnlyList<MetadataEvidence> evidence,
            string field)
        {
            if (!string.IsNullOrWhiteSpace(metadataValue))
                return metadataValue.Trim();

            return evidence
                .Where(item =>
                    string.Equals(item.Field, field, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(item.Value))
                .Select(item => item.Value.Trim())
                .FirstOrDefault() ?? string.Empty;
        }

        private static string GetProviderName(string source)
        {
            if (source.Contains("google", StringComparison.OrdinalIgnoreCase))
                return "Google Books";

            if (source.Contains("openlibrary", StringComparison.OrdinalIgnoreCase))
                return "Open Library";

            return source;
        }

        private static string GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out JsonElement value) &&
                   value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }

        private static List<string> GetStringArray(JsonElement element, string propertyName)
        {
            List<string> values = new();

            if (!element.TryGetProperty(propertyName, out JsonElement value) ||
                value.ValueKind != JsonValueKind.Array)
            {
                return values;
            }

            foreach (JsonElement item in value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                    continue;

                string text = item.GetString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(text))
                    values.Add(text.Trim());
            }

            return values;
        }

        private static List<string> GetStringArrayOrNumbers(
            JsonElement element,
            string propertyName)
        {
            List<string> values = new();

            if (!element.TryGetProperty(propertyName, out JsonElement value) ||
                value.ValueKind != JsonValueKind.Array)
            {
                return values;
            }

            foreach (JsonElement item in value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    string text = item.GetString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(text))
                        values.Add(text.Trim());
                }
                else if (item.ValueKind == JsonValueKind.Number)
                {
                    values.Add(item.ToString());
                }
            }

            return values;
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
                string type = GetString(id, "type");
                string identifier = GetString(id, "identifier");

                if (!string.Equals(type, "ISBN_10", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(type, "ISBN_13", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(identifier))
                    values.Add(identifier.Trim());
            }

            return values;
        }

        private static void AddJsonArrayValues(
            JsonElement root,
            string propertyName,
            List<string> destination)
        {
            if (!root.TryGetProperty(propertyName, out JsonElement array) ||
                array.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (JsonElement item in array.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    destination.Add(item.GetString() ?? string.Empty);
            }
        }

        private static IReadOnlyList<string> SplitAuthors(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<string>();

            return value
                .Split(
                    new[] { ';', '|', '&' },
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .SelectMany(part =>
                    part.Contains(" and ", StringComparison.OrdinalIgnoreCase)
                        ? part.Split(
                            new[] { " and " },
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        : new[] { part })
                .ToArray();
        }

        private static string NormalizeIsbn(string isbn)
        {
            return isbn
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty)
                .Trim()
                .ToUpperInvariant();
        }

        private static bool IsValidIsbn(string isbn)
        {
            if (isbn.Length == 13)
            {
                if (!isbn.StartsWith("978", StringComparison.Ordinal) &&
                    !isbn.StartsWith("979", StringComparison.Ordinal))
                {
                    return false;
                }

                int sum = 0;
                for (int i = 0; i < 13; i++)
                {
                    if (!char.IsDigit(isbn[i]))
                        return false;

                    int digit = isbn[i] - '0';
                    sum += i % 2 == 0 ? digit : digit * 3;
                }

                return sum % 10 == 0;
            }

            if (isbn.Length == 10)
            {
                int sum = 0;
                for (int i = 0; i < 10; i++)
                {
                    int value;
                    if (isbn[i] == 'X' && i == 9)
                        value = 10;
                    else if (char.IsDigit(isbn[i]))
                        value = isbn[i] - '0';
                    else
                        return false;

                    sum += value * (10 - i);
                }

                return sum % 11 == 0;
            }

            return false;
        }

        private static string NormalizeText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return new string(
                    value
                        .ToLowerInvariant()
                        .Where(character => char.IsLetterOrDigit(character) || char.IsWhiteSpace(character))
                        .ToArray())
                .Trim();
        }

        private static string ExtractPublicationYear(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            for (int i = 0; i <= value.Length - 4; i++)
            {
                string part = value.Substring(i, 4);

                if (part.All(char.IsDigit) && part[0] >= '1' && part[0] <= '2')
                    return part;
            }

            return string.Empty;
        }

        private static HttpClient CreateHttpClient()
        {
            HttpClient client = new()
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("Scout-EbookExpert", "1.0"));

            return client;
        }

        private static IsbnEditionVerification VerifyIsbnEdition(
            string isbn,
            E_EbookMetadata metadata)
        {
            if (string.IsNullOrWhiteSpace(isbn))
                return new IsbnEditionVerification();

            try
            {
                string requestUrl =
                    "https://openlibrary.org/isbn/" +
                    Uri.EscapeDataString(isbn) +
                    ".json";

                ProviderFetchResult fetch = FetchJson(requestUrl, "Open Library ISBN verification");

                if (string.IsNullOrWhiteSpace(fetch.Json))
                    return new IsbnEditionVerification();

                using JsonDocument document = JsonDocument.Parse(fetch.Json);
                JsonElement root = document.RootElement;

                string title = GetString(root, "title");
                string publisher = GetStringArray(root, "publishers").FirstOrDefault() ?? string.Empty;
                string publicationDate = GetString(root, "publish_date");
                string editionKey = GetString(root, "key");

                return new IsbnEditionVerification
                {
                    Found = true,
                    Isbn = isbn,
                    Title = title,
                    Publisher = publisher,
                    PublicationDate = publicationDate,
                    EditionKey = editionKey
                };
            }
            catch
            {
                return new IsbnEditionVerification();
            }
        }

        private sealed class ProviderFetchResult
        {
            public bool Responded { get; init; }
            public bool Succeeded { get; init; }
            public bool TimedOut { get; init; }
            public bool RateLimited { get; init; }
            public int RetryAfterSeconds { get; init; }
            public string? Json { get; init; }
            public string Diagnostic { get; init; } = string.Empty;
        }

        private sealed class IsbnEditionRecord
        {
            public string Title { get; init; } = string.Empty;
            public string Publisher { get; init; } = string.Empty;
            public string PublicationDate { get; init; } = string.Empty;
            public List<string> Isbns { get; init; } = new();
        }

        private sealed class IsbnEditionVerification
        {
            public bool Found { get; init; }
            public string Isbn { get; init; } = string.Empty;
            public string Title { get; init; } = string.Empty;
            public string Publisher { get; init; } = string.Empty;
            public string PublicationDate { get; init; } = string.Empty;
            public string EditionKey { get; init; } = string.Empty;
        }
    }

    internal enum IsbnResearchStatus
    {
        Succeeded,
        NoCandidates,
        TimedOut,
        ProviderUnavailable,
        RateLimited
    }

    internal sealed class IsbnResearchResult
    {
        public IsbnResearchStatus Status { get; init; } = IsbnResearchStatus.NoCandidates;
        public List<IsbnResearchCandidate> Candidates { get; init; } = new();
        public int RetryAfterSeconds { get; init; }
        public List<string> Diagnostics { get; init; } = new();
    }

    internal sealed class IsbnResearchCandidate
    {
        public string Isbn { get; init; } = string.Empty;
        public string EditionKey { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Author { get; init; } = string.Empty;
        public string Publisher { get; init; } = string.Empty;
        public string PublicationYear { get; init; } = string.Empty;
        public bool EditionVerified { get; init; }
        public string VerifiedEditionTitle { get; init; } = string.Empty;
        public string VerifiedEditionPublisher { get; init; } = string.Empty;
        public string VerifiedEditionPublicationDate { get; init; } = string.Empty;
        public string VerifiedEditionKey { get; init; } = string.Empty;
        public string Source { get; init; } = string.Empty;
        public string Evidence { get; init; } = string.Empty;
        public double Confidence { get; init; }
    }
}
