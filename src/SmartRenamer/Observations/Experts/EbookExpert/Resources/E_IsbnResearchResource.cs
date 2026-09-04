using SmartRenamer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text.Json;
using System.Threading;

namespace SmartRenamer.Observations.Experts.EbookExpert.Resources
{
    /// <summary>
    /// =========================================================================
    /// E_IsbnResearchResource
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Researches possible ISBN values for an ebook whose ISBN metadata is
    /// missing.
    ///
    /// Current Research Source
    /// -------------------------------------------------------------------------
    /// Open Library Search API.
    ///
    /// Research uses the metadata already known about the ebook, primarily
    /// title and author, to locate matching editions and their ISBN values.
    ///
    /// This Resource does NOT
    /// -------------------------------------------------------------------------
    /// • Decide whether an ISBN is missing.
    /// • Decide whether research should occur.
    /// • Select the final ISBN.
    /// • Approve a repair.
    /// • Modify an EPUB.
    /// • Communicate with Scout.
    /// • Generate recommendations.
    ///
    /// Those responsibilities belong to the Repair Investigation,
    /// Repair Service, Conversation Framework, and E_EpubRepairResource.
    ///
    /// =========================================================================
    /// </summary>
    internal sealed class E_IsbnResearchResource
    {
        private static readonly HttpClient HttpClient = CreateHttpClient();

        /// <summary>
        /// Researches possible ISBN values using the metadata already
        /// available for the ebook.
        ///
        /// The Resource returns candidates rather than selecting an ISBN.
        /// No ebook is modified by this operation.
        /// </summary>
        public List<IsbnResearchCandidate> Research(
            E_EbookMetadata metadata)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            //---------------------------------------------------------
            // There must be enough identifying information to perform
            // a useful book search.
            //---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(metadata.Title) &&
                string.IsNullOrWhiteSpace(metadata.Author))
            {
                return new List<IsbnResearchCandidate>();
            }

            try
            {
                string requestUrl =
                    BuildSearchUrl(metadata);

                using HttpRequestMessage request =
                    new(
                        HttpMethod.Get,
                        requestUrl);

                using HttpResponseMessage response =
                    HttpClient
                        .Send(
                            request,
                            HttpCompletionOption.ResponseHeadersRead,
                            CancellationToken.None);

                if (!response.IsSuccessStatusCode)
                    return new List<IsbnResearchCandidate>();

                string json =
                    response.Content
                        .ReadAsStringAsync()
                        .GetAwaiter()
                        .GetResult();

                return ParseCandidates(
                    json,
                    metadata,
                    requestUrl);
            }
            catch
            {
                //---------------------------------------------------------
                // Research failure must not damage the ebook or cause
                // the Expert to make an unsupported ISBN claim.
                //---------------------------------------------------------

                return new List<IsbnResearchCandidate>();
            }
        }

        /// <summary>
        /// Creates the Open Library Search API request.
        /// </summary>
        private static string BuildSearchUrl(
            E_EbookMetadata metadata)
        {
            List<string> parameters = new();

            if (!string.IsNullOrWhiteSpace(metadata.Title))
            {
                parameters.Add(
                    "title=" +
                    Uri.EscapeDataString(
                        metadata.Title.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(metadata.Author))
            {
                parameters.Add(
                    "author=" +
                    Uri.EscapeDataString(
                        metadata.Author.Trim()));
            }

            parameters.Add(
    "fields=" +
    Uri.EscapeDataString(
        "title,author_name,isbn,edition_key,publisher,publish_year"));

            parameters.Add("limit=10");

            return
                "https://openlibrary.org/search.json?" +
                string.Join("&", parameters);
        }

        /// <summary>
        /// Converts Open Library search results into ISBN candidates.
        ///
        /// Candidates are deduplicated. ISBNs are not selected or approved
        /// here.
        /// </summary>
        private static List<IsbnResearchCandidate> ParseCandidates(
            string json,
            E_EbookMetadata metadata,
            string sourceUrl)
        {
            List<IsbnResearchCandidate> candidates = new();

            using JsonDocument document =
                JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty(
                    "docs",
                    out JsonElement docs) ||
                docs.ValueKind != JsonValueKind.Array)
            {
                return candidates;
            }

            HashSet<string> seenIsbns =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (JsonElement doc in docs.EnumerateArray())
            {
                string title =
                    GetString(
                        doc,
                        "title");

                List<string> authors =
                    GetStringArray(
                        doc,
                        "author_name");

                List<string> publishers =
                    GetStringArray(
                        doc,
                        "publisher");

                string publisher =
                    publishers.Count > 0
                        ? publishers[0]
                        : string.Empty;

                List<string> publicationYears =
                    GetStringArrayOrNumbers(
                        doc,
                        "publish_year");

                string publicationYear =
                    publicationYears.Count > 0
                        ? publicationYears[0]
                        : string.Empty;

                List<string> editionKeys =
                    GetStringArray(
                        doc,
                        "edition_key");

                string editionKey =
                    editionKeys.Count > 0
                        ? editionKeys[0]
                        : string.Empty;
                double confidence =
                    CalculateConfidence(
                        metadata,
                        title,
                        authors,
                        publisher);

                //---------------------------------------------------------
                // We do not want weak matches becoming repair candidates.
                //---------------------------------------------------------

                if (confidence < 0.50)
                    continue;

                List<string> isbns =
                    GetStringArray(
                        doc,
                        "isbn");

                foreach (string isbnValue in isbns)
                {
                    string isbn =
                        NormalizeIsbn(isbnValue);

                    if (!IsValidIsbn(isbn))
                        continue;

                    if (!seenIsbns.Add(isbn))
                        continue;

                    IsbnEditionVerification verification =
                        VerifyIsbnEdition(
                            isbn,
                            metadata);

                    confidence =
    CalculateVerifiedConfidence(
        metadata,
        confidence,
        publicationYear,
        verification);

                    candidates.Add(
                        new IsbnResearchCandidate
                        {
                            Isbn = isbn,
                            EditionKey =
    verification.Found
        ? verification.EditionKey
        : editionKey,
                            Title =
    verification.Found &&
    !string.IsNullOrWhiteSpace(
        verification.Title)
        ? verification.Title
        : title,
                            Author = string.Join(", ", authors),
                            Publisher =
    verification.Found &&
    !string.IsNullOrWhiteSpace(
        verification.Publisher)
        ? verification.Publisher
        : publisher,
                            PublicationYear =
    verification.Found &&
    !string.IsNullOrWhiteSpace(
        verification.PublicationDate)
        ? verification.PublicationDate
        : publicationYear,
                            Source = sourceUrl,
                            Evidence =
    BuildEvidence(
        title,
        authors,
        publisher,
        publicationYear,
        confidence)
    + " " +
    BuildEditionEvidence(
        verification),
                            EditionVerified = verification.Found,
                            VerifiedEditionTitle = verification.Title,
                            VerifiedEditionPublisher = verification.Publisher,
                            VerifiedEditionPublicationDate = verification.PublicationDate,
                            VerifiedEditionKey = verification.EditionKey,
                            Confidence = confidence
                        });
                }
            }

            candidates.Sort(
                (left, right) =>
                    right.Confidence.CompareTo(
                        left.Confidence));

            return candidates;



        }

        /// <summary>
        /// Calculates confidence from the metadata already known about
        /// the ebook.
        ///
        /// Exact title and author matches receive the strongest confidence.
        /// </summary>
        private static double CalculateConfidence(
            E_EbookMetadata metadata,
            string resultTitle,
            List<string> resultAuthors,
            string resultPublisher)
        {
            double score = 0.0;

            string sourceTitle =
                NormalizeText(metadata.Title);

            string foundTitle =
                NormalizeText(resultTitle);

            if (!string.IsNullOrWhiteSpace(sourceTitle) &&
                !string.IsNullOrWhiteSpace(foundTitle))
            {
                if (string.Equals(
                        sourceTitle,
                        foundTitle,
                        StringComparison.OrdinalIgnoreCase))
                {
                    score += 0.50;
                }
                else if (foundTitle.Contains(sourceTitle) ||
                         sourceTitle.Contains(foundTitle))
                {
                    score += 0.30;
                }
            }

            string sourceAuthor =
                NormalizeText(metadata.Author);

            if (!string.IsNullOrWhiteSpace(sourceAuthor) &&
                resultAuthors.Count > 0)
            {
                foreach (string author in resultAuthors)
                {
                    string foundAuthor =
                        NormalizeText(author);

                    if (string.Equals(
                            sourceAuthor,
                            foundAuthor,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        score += 0.25;
                        break;
                    }

                    if (foundAuthor.Contains(sourceAuthor) ||
                        sourceAuthor.Contains(foundAuthor))
                    {
                        score += 0.15;
                        break;
                    }
                }
            }

            string sourcePublisher =
                NormalizeText(metadata.Publisher);

            string foundPublisher =
                NormalizeText(resultPublisher);

            if (!string.IsNullOrWhiteSpace(sourcePublisher) &&
                !string.IsNullOrWhiteSpace(foundPublisher))
            {
                if (string.Equals(
                        sourcePublisher,
                        foundPublisher,
                        StringComparison.OrdinalIgnoreCase))
                {
                    score += 0.25;
                }
                else if (foundPublisher.Contains(sourcePublisher) ||
                         sourcePublisher.Contains(foundPublisher))
                {
                    score += 0.15;
                }

            }


                return Math.Min(
                    score,
                    1.0);
            }


        /// <summary>
        /// Strengthens or rejects a search candidate using the
        /// ISBN-specific edition verification.
        /// </summary>
        private static double CalculateVerifiedConfidence(
    E_EbookMetadata metadata,
    double searchConfidence,
    string searchPublicationYear,
    IsbnEditionVerification verification)
        {
            if (!verification.Found)
                return searchConfidence;

            double score =
                searchConfidence;

            string sourceTitle =
                NormalizeText(
                    metadata.Title);

            string verifiedTitle =
                NormalizeText(
                    verification.Title);

            if (!string.IsNullOrWhiteSpace(sourceTitle) &&
                !string.IsNullOrWhiteSpace(verifiedTitle))
            {
                if (string.Equals(
                        sourceTitle,
                        verifiedTitle,
                        StringComparison.OrdinalIgnoreCase))
                {
                    score += 0.15;
                }
                else if (verifiedTitle.Contains(sourceTitle) ||
                         sourceTitle.Contains(verifiedTitle))
                {
                    score += 0.08;
                }
            }

            string sourcePublisher =
                NormalizeText(
                    metadata.Publisher);

            string verifiedPublisher =
                NormalizeText(
                    verification.Publisher);

            if (!string.IsNullOrWhiteSpace(sourcePublisher) &&
                !string.IsNullOrWhiteSpace(verifiedPublisher))
            {
                if (string.Equals(
                        sourcePublisher,
                        verifiedPublisher,
                        StringComparison.OrdinalIgnoreCase))
                {
                    score += 0.15;
                }
                else if (verifiedPublisher.Contains(sourcePublisher) ||
                         sourcePublisher.Contains(verifiedPublisher))
                {
                    score += 0.08;
                }



            }

            string searchYear =
    ExtractPublicationYear(searchPublicationYear);

            string verifiedYear =
                ExtractPublicationYear(
                    verification.PublicationDate);

            if (!string.IsNullOrWhiteSpace(searchYear) &&
                !string.IsNullOrWhiteSpace(verifiedYear))
            {
                if (string.Equals(
                        searchYear,
                        verifiedYear,
                        StringComparison.OrdinalIgnoreCase))
                {
                    score += 0.10;
                }
                else
                {
                    score -= 0.20;
                }
            }
            return Math.Min(
                score,
                1.0);
        }
        /// <summary>
        /// Produces human-readable evidence describing the research match.
        /// </summary>
        private static string BuildEvidence(
    string title,
    List<string> authors,
    string publisher,
    string publicationYear,
    double confidence)
        {
            string authorText =
                authors.Count > 0
                    ? string.Join(", ", authors)
                    : "unknown author";

            return
                $"Open Library match: \"{title}\" by {authorText}. " +
                $"Publisher: {publisher}. " +
                $"Publication year: {publicationYear}. " +
                $"Match confidence: {confidence:0.00}.";
        }
        /// <summary>
        /// Produces evidence describing the edition-specific ISBN verification.
        /// </summary>
        private static string BuildEditionEvidence(
            IsbnEditionVerification verification)
        {
            if (!verification.Found)
            {
                return
                    "ISBN-specific edition verification was not available.";
            }

            string title =
                string.IsNullOrWhiteSpace(
                    verification.Title)
                    ? "unknown title"
                    : verification.Title;

            string publisher =
                string.IsNullOrWhiteSpace(
                    verification.Publisher)
                    ? "unknown publisher"
                    : verification.Publisher;

            string publicationDate =
                string.IsNullOrWhiteSpace(
                    verification.PublicationDate)
                    ? "unknown publication date"
                    : verification.PublicationDate;

            string editionKey =
                string.IsNullOrWhiteSpace(
                    verification.EditionKey)
                    ? "unknown edition key"
                    : verification.EditionKey;

            return
                $"ISBN-specific verification: {verification.Isbn}. " +
                $"Edition: \"{title}\". " +
                $"Publisher: {publisher}. " +
                $"Publication date: {publicationDate}. " +
                $"Edition key: {editionKey}.";
        }
        /// <summary>
        /// Reads a string property from a JSON document.
        /// </summary>
        private static string GetString(
            JsonElement element,
            string propertyName)
        {
            if (!element.TryGetProperty(
                    propertyName,
                    out JsonElement value))
            {
                return string.Empty;
            }

            return value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }

        /// <summary>
        /// Reads an array of strings from a JSON document.
        /// </summary>
        private static List<string> GetStringArray(
            JsonElement element,
            string propertyName)
        {
            List<string> values = new();

            if (!element.TryGetProperty(
                    propertyName,
                    out JsonElement value) ||
                value.ValueKind != JsonValueKind.Array)
            {
                return values;
            }



            foreach (JsonElement item in value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                    continue;

                string? text =
                    item.GetString();

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

            if (!element.TryGetProperty(
                    propertyName,
                    out JsonElement value) ||
                value.ValueKind != JsonValueKind.Array)
            {
                return values;
            }

            foreach (JsonElement item in value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    string? text =
                        item.GetString();

                    if (!string.IsNullOrWhiteSpace(text))
                        values.Add(text.Trim());

                    continue;
                }

                if (item.ValueKind == JsonValueKind.Number)
                {
                    values.Add(
                        item.ToString());
                }
            }

            return values;
        }
        /// <summary>
        /// Normalizes an ISBN by removing punctuation and spaces.
        /// </summary>
        private static string NormalizeIsbn(
            string isbn)
        {
            return isbn
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty)
                .Trim()
                .ToUpperInvariant();
        }

        /// <summary>
        /// Validates ISBN-10 and ISBN-13 values.
        /// </summary>
        private static bool IsValidIsbn(
            string isbn)
        {
            if (isbn.Length == 13)
            {
                if (!isbn.StartsWith("978") &&
                    !isbn.StartsWith("979"))
                {
                    return false;
                }

                int sum = 0;

                for (int i = 0; i < 13; i++)
                {
                    if (!char.IsDigit(isbn[i]))
                        return false;

                    int digit =
                        isbn[i] - '0';

                    sum +=
                        i % 2 == 0
                            ? digit
                            : digit * 3;
                }

                return sum % 10 == 0;
            }

            if (isbn.Length == 10)
            {
                int sum = 0;

                for (int i = 0; i < 10; i++)
                {
                    int value;

                    if (isbn[i] == 'X' &&
                        i == 9)
                    {
                        value = 10;
                    }
                    else if (char.IsDigit(isbn[i]))
                    {
                        value =
                            isbn[i] - '0';
                    }
                    else
                    {
                        return false;
                    }

                    sum +=
                        value * (10 - i);
                }

                return sum % 11 == 0;
            }

            return false;
        }

        /// <summary>
        /// Normalizes text for conservative title/author comparison.
        /// </summary>
        private static string NormalizeText(
            string? value)



        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return new string(
                    value
                        .ToLowerInvariant()
                        .Where(
                            character =>
                                char.IsLetterOrDigit(character) ||
                                char.IsWhiteSpace(character))
                        .ToArray())
                .Trim();
        }

        /// <summary>
        /// Creates the shared HTTP client used for Open Library requests.
        /// </summary>
        /// 

        private static string ExtractPublicationYear(
    string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            for (int i = 0; i <= value.Length - 4; i++)
            {
                string part =
                    value.Substring(i, 4);

                if (part.All(char.IsDigit) &&
                    part[0] >= '1' &&
                    part[0] <= '2')
                {
                    return part;
                }
            }

            return string.Empty;
        }

        private static HttpClient CreateHttpClient()
        {
            HttpClient client = new();

            client.DefaultRequestHeaders.UserAgent.Clear();

            client.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(
                    "SmartRenamer",
                    "1.0"));

            return client;
        }
        /// <summary>
        /// Looks up a specific ISBN through Open Library's ISBN endpoint.
        ///
        /// Unlike the Search API, this request is tied to the supplied ISBN
        /// and therefore identifies the edition associated with that ISBN.
        ///
        /// No ebook is modified by this operation.
        /// </summary>
        private static IsbnEditionVerification VerifyIsbnEdition(
            string isbn,
            E_EbookMetadata metadata)
        {
            if (string.IsNullOrWhiteSpace(isbn))
            {
                return new IsbnEditionVerification();
            }

            try
            {
                string requestUrl =
                    "https://openlibrary.org/isbn/" +
                    Uri.EscapeDataString(isbn) +
                    ".json";

                using HttpRequestMessage request =
                    new(
                        HttpMethod.Get,
                        requestUrl);

                using HttpResponseMessage response =
                    HttpClient
                        .Send(
                            request,
                            HttpCompletionOption.ResponseHeadersRead,
                            CancellationToken.None);

                if (!response.IsSuccessStatusCode)
                {
                    return new IsbnEditionVerification();
                }

                string json =
                    response.Content
                        .ReadAsStringAsync()
                        .GetAwaiter()
                        .GetResult();

                using JsonDocument document =
                    JsonDocument.Parse(json);

                JsonElement root =
                    document.RootElement;

                string title =
                    GetString(
                        root,
                        "title");

                List<string> publishers =
                    GetStringArray(
                        root,
                        "publishers");

                string publisher =
                    publishers.Count > 0
                        ? publishers[0]
                        : string.Empty;

                string publicationDate =
                    GetString(
                        root,
                        "publish_date");

                string editionKey =
                    GetString(
                        root,
                        "key");

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
        /// <summary>
        /// Represents the edition information returned by an ISBN-specific
        /// Open Library lookup.
        /// </summary>
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

    /// <summary>
    /// Represents one ISBN candidate discovered during research.
    /// </summary>
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