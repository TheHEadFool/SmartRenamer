using SmartRenamer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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

                string publisher =
                GetString(
                    doc,
                    "publisher");

                List<string> publicationYears =
                    GetStringArray(
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
                        authors);

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
                                    title,
                                    authors,
                                    confidence),
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
            List<string> resultAuthors)
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
                    score += 0.70;
                }
                else if (foundTitle.Contains(sourceTitle) ||
                         sourceTitle.Contains(foundTitle))
                {
                    score += 0.45;
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
                        score += 0.30;
                        break;
                    }

                    if (foundAuthor.Contains(sourceAuthor) ||
                        sourceAuthor.Contains(foundAuthor))
                    {
                        score += 0.20;
                        break;
                    }
                }
            }

            return Math.Min(score, 1.0);
        }

        /// <summary>
        /// Produces human-readable evidence describing the research match.
        /// </summary>
        private static string BuildEvidence(
            string title,
            List<string> authors,
            double confidence)
        {
            string authorText =
                authors.Count > 0
                    ? string.Join(", ", authors)
                    : "unknown author";

            return
                $"Open Library match: \"{title}\" by {authorText}. " +
                $"Match confidence: {confidence:0.00}.";
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

        public string Source { get; init; } = string.Empty;

        public string Evidence { get; init; } = string.Empty;

        public double Confidence { get; init; }
    }
}