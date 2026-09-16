using Scout.Observations.Experts.EbookExpert.Data;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;
using SmartRenamer.Observations.Experts.EbookExpert.Resources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scout.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// Evaluates ISBN research candidates against additional evidence
    /// available from the EPUB itself.
    ///
    /// This evaluator is responsible for interpreting ISBN-specific evidence.
    /// It does not perform repairs, communicate with Scout, or make changes
    /// to the EPUB.
    /// </summary>
    internal sealed class E_IsbnRepairEvidenceEvaluator
    {
        private readonly E_EpubContentResource _contentResource = new();

        /// <summary>
        /// Evaluates ISBN candidates using the EPUB metadata and, when needed,
        /// the opening content of the EPUB.
        /// </summary>
        /// <param name="metadata">
        /// The metadata currently known for the EPUB.
        /// </param>
        /// <param name="candidates">
        /// ISBN candidates produced by the ISBN research resource.
        /// </param>
        /// <param name="maxDocuments">
        /// Maximum number of opening reading-order documents to inspect.
        /// </param>
        public List<RepairDecisionCandidate> Evaluate(
    E_EbookMetadata metadata,
    string epubPath,
    IReadOnlyList<IsbnResearchCandidate> candidates,
    int maxDocuments = 10)
        {
            ArgumentNullException.ThrowIfNull(metadata);
            ArgumentNullException.ThrowIfNull(candidates);
            ArgumentException.ThrowIfNullOrWhiteSpace(epubPath);

            if (candidates.Count == 0)
                return new List<RepairDecisionCandidate>();

            string content =
                _contentResource.ExtractOpeningText(
                    epubPath,
                    maxDocuments);

            return candidates
                .Select(
                    candidate =>
                        EvaluateCandidate(
                            metadata,
                            candidate,
                            content))
                .ToList();
        }

        /// <summary>
        /// Evaluates one ISBN candidate against the available EPUB evidence.
        /// </summary>
        private static RepairDecisionCandidate EvaluateCandidate(
            E_EbookMetadata metadata,
            IsbnResearchCandidate candidate,
            string content)
        {
            bool isbnAppearsInContent =
                ContainsNormalizedIsbn(
                    content,
                    candidate.Isbn);

            bool titleAppearsInContent =
                ContainsNormalizedText(
                    content,
                    candidate.Title);

            bool authorAppearsInContent =
                ContainsNormalizedText(
                    content,
                    candidate.Author);

            bool publisherAppearsInContent =
                ContainsNormalizedText(
                    content,
                    candidate.Publisher);

            int supportingMatches =
                0;

            if (isbnAppearsInContent)
                supportingMatches++;

            if (titleAppearsInContent)
                supportingMatches++;

            if (authorAppearsInContent)
                supportingMatches++;

            if (publisherAppearsInContent)
                supportingMatches++;

            string evidence =
                BuildEvidence(
                    candidate,
                    isbnAppearsInContent,
                    titleAppearsInContent,
                    authorAppearsInContent,
                    publisherAppearsInContent);

            RepairDecisionCandidate evaluatedCandidate = new()
            {
                Value = candidate.Isbn,
                Source = candidate.Source,
                Evidence = evidence,
                Confidence = candidate.Confidence,
                IsPreferred =
                    isbnAppearsInContent
            };

            AddCandidateDetail(
                evaluatedCandidate,
                "ISBN",
                candidate.Isbn);

            AddCandidateDetail(
                evaluatedCandidate,
                "Title",
                candidate.Title);

            AddCandidateDetail(
                evaluatedCandidate,
                "Author",
                candidate.Author);

            AddCandidateDetail(
                evaluatedCandidate,
                "Publisher",
                candidate.Publisher);

            AddCandidateDetail(
                evaluatedCandidate,
                "Publication year",
                candidate.PublicationYear);

            if (candidate.EditionVerified)
            {
                AddCandidateDetail(
                    evaluatedCandidate,
                    "Edition verification",
                    "Independently verified");
            }

            return evaluatedCandidate;
        }

        /// <summary>
        /// Adds one factual candidate detail when the research resource
        /// supplied a value for it.
        /// </summary>
        private static void AddCandidateDetail(
            RepairDecisionCandidate candidate,
            string label,
            string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            candidate.Details.Add(
                $"{label}: {value.Trim()}");
        }

        /// <summary>
        /// Determines whether an ISBN appears in EPUB content after
        /// normalizing punctuation and whitespace.
        /// </summary>
        private static bool ContainsNormalizedIsbn(
            string content,
            string isbn)
        {
            if (string.IsNullOrWhiteSpace(content) ||
                string.IsNullOrWhiteSpace(isbn))
            {
                return false;
            }

            string normalizedContent =
                new string(
                    content
                        .Where(char.IsLetterOrDigit)
                        .ToArray())
                    .ToUpperInvariant();

            string normalizedIsbn =
                isbn
                    .Replace("-", string.Empty)
                    .Replace(" ", string.Empty)
                    .Trim()
                    .ToUpperInvariant();

            return normalizedContent.Contains(
                normalizedIsbn,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Determines whether a non-empty text value appears in the EPUB
        /// content after conservative normalization.
        /// </summary>
        private static bool ContainsNormalizedText(
            string content,
            string value)
        {
            if (string.IsNullOrWhiteSpace(content) ||
                string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string normalizedContent =
                NormalizeText(content);

            string normalizedValue =
                NormalizeText(value);

            return !string.IsNullOrWhiteSpace(normalizedValue) &&
                   normalizedContent.Contains(
                       normalizedValue,
                       StringComparison.Ordinal);
        }

        /// <summary>
        /// Produces human-readable evidence describing what the EPUB itself
        /// supported about the candidate.
        /// </summary>
        private static string BuildEvidence(
            IsbnResearchCandidate candidate,
            bool isbnAppearsInContent,
            bool titleAppearsInContent,
            bool authorAppearsInContent,
            bool publisherAppearsInContent)
        {
            List<string> evidence = new();

            if (isbnAppearsInContent)
                evidence.Add(
                    $"The candidate ISBN {candidate.Isbn} appears in the EPUB opening content.");

            if (titleAppearsInContent)
                evidence.Add(
                    "The candidate title appears in the EPUB opening content.");

            if (authorAppearsInContent)
                evidence.Add(
                    "The candidate author appears in the EPUB opening content.");

            if (publisherAppearsInContent)
                evidence.Add(
                    "The candidate publisher appears in the EPUB opening content.");

            if (candidate.EditionVerified)
                evidence.Add(
                    "The candidate ISBN was independently verified through the ISBN-specific research lookup.");

            if (evidence.Count == 0)
            {
                evidence.Add(
                    "No additional matching identity evidence was found in the EPUB opening content.");
            }

            return string.Join(" ", evidence);
        }

        /// <summary>
        /// Applies conservative normalization for textual comparisons.
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
    }
}