using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Scout.Observations.Experts.EbookExpert.Data;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// Evaluates local Title, Author, Series, and SeriesNumber identity evidence.
    ///
    /// The evaluator never modifies an EPUB, performs external research, or
    /// silently converts a clue into truth. It only establishes candidates
    /// when the available evidence supports them.
    /// </summary>
    internal sealed class E_BookIdentityEvaluator
    {
        private static readonly Regex ExplicitTitlePattern =
            new(
                @"\btitle\s*(?:is|=|:)\s*(?<value>[^;\r\n]+)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ExplicitAuthorPattern =
            new(
                @"\bauthor(?:s)?\s*(?:is|=|:)\s*(?<value>[^;\r\n]+)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Opening-content evidence is internal evidence from the book itself.
        // These patterns intentionally require an explicit label; ordinary
        // prose is never treated as a title or author merely because it looks
        // plausible.
        private static readonly Regex ExplicitOpeningTitlePattern =
            new(
                @"\b(?:book\s+)?title\s*(?:is|=|:)\s*(?<value>.+?)(?=\s+(?:author|authors)\s*(?:is|=|:)|\s+written\s+by\s+|\s+(?:publisher|isbn|series)\s*(?:is|=|:)|$)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ExplicitOpeningAuthorPattern =
            new(
                @"(?:\b(?:author|authors)\s*(?:is|=|:)\s*|\bwritten\s+by\s+)(?<value>.+?)(?=\s+(?:title|publisher|isbn|series)\s*(?:is|=|:)|$)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ExplicitSeriesNumberPattern =
            new(
                @"\b(?:series\s*number|number)\s*(?:is|=|:)\s*(?<value>\d+(?:\.\d+)?)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Recognizes an explicit filename series structure with an optional
        // leading collection/filename position. The leading number is NOT
        // treated as SeriesNumber. Only the number attached to the series
        // expression (the '#02' portion) is series-position evidence.
        private static readonly Regex ExplicitFilenameSeriesIdentityPattern =
            new(
                @"^(?:(?<collectionNumber>\d+(?:\.\d+)?)\s*-\s*)?(?<series>.+?)\s+#\s*(?<number>\d+(?:\.\d+)?)\s+-\s+(?<title>.+)$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly E_SeriesEvidenceEvaluator _seriesEvaluator = new();

        public BookIdentityEvaluation Evaluate(
            MetadataRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);

            E_EbookMetadata metadata = record.Metadata;

            SeriesEvidenceEvaluation seriesEvaluation =
                _seriesEvaluator.Evaluate(record);

            //---------------------------------------------------------
            // Start with what the EPUB actually contains. A candidate only
            // replaces an observed value when the evidence evaluator has
            // established a better local candidate.
            //---------------------------------------------------------

            string candidateTitle = metadata.Title ?? "";
            string candidateAuthor = metadata.Author ?? "";
            string candidateSeries = metadata.Series ?? "";
            string candidateSeriesNumber = metadata.SeriesNumber ?? "";

            List<string> evidence = new();

            string? explicitUserTitle =
                GetSingleExplicitUserValue(
                    record,
                    ExplicitTitlePattern,
                    "Title");

            string? explicitUserAuthor =
                GetSingleExplicitUserValue(
                    record,
                    ExplicitAuthorPattern,
                    "Author");

            string? explicitUserSeriesNumber =
                GetSingleExplicitUserValue(
                    record,
                    ExplicitSeriesNumberPattern,
                    "SeriesNumber");

            bool explicitUserIdentityConflict =
                HasConflictingExplicitUserValues(
                    record,
                    ExplicitTitlePattern,
                    "Title") ||
                HasConflictingExplicitUserValues(
                    record,
                    ExplicitAuthorPattern,
                    "Author") ||
                HasConflictingExplicitUserValues(
                    record,
                    ExplicitSeriesNumberPattern,
                    "SeriesNumber");

            if (!string.IsNullOrWhiteSpace(explicitUserTitle))
            {
                candidateTitle = explicitUserTitle;
                evidence.Add(
                    $"User-supplied Title evidence: '{explicitUserTitle}'.");
            }

            if (!string.IsNullOrWhiteSpace(explicitUserAuthor))
            {
                candidateAuthor = explicitUserAuthor;
                evidence.Add(
                    $"User-supplied Author evidence: '{explicitUserAuthor}'.");
            }

            if (!string.IsNullOrWhiteSpace(explicitUserSeriesNumber))
            {
                candidateSeriesNumber = explicitUserSeriesNumber;
                evidence.Add(
                    $"User-supplied SeriesNumber evidence: '{explicitUserSeriesNumber}'.");
            }

            if (explicitUserIdentityConflict)
            {
                evidence.Add(
                    "Conflicting explicit user identity values were supplied; Scout will not automatically choose between them.");
            }

            //---------------------------------------------------------
            // Internal book evidence comes before filename evidence.
            //
            // The filename is mutable and therefore cannot outrank what the
            // EPUB itself says. When the opening content explicitly identifies
            // the title or author, that evidence is allowed to establish the
            // local candidate before the filename is examined. The filename
            // may corroborate it, but does not silently replace it.
            //---------------------------------------------------------

            bool internalTitleCorrectionDetected = false;
            bool internalAuthorCorrectionDetected = false;

            IReadOnlyList<string> openingContentEvidence =
                record.Evidence
                    .Where(item =>
                        string.Equals(
                            item.Source,
                            "Opening Content",
                            StringComparison.OrdinalIgnoreCase))
                    .Select(item => item.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .ToList();

            foreach (string openingContent in openingContentEvidence)
            {
                if (string.IsNullOrWhiteSpace(explicitUserTitle))
                {
                    Match openingTitle =
                        ExplicitOpeningTitlePattern.Match(openingContent);

                    if (openingTitle.Success)
                    {
                        string value =
                            CleanIdentityValue(openingTitle.Groups["value"].Value);

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            if (!SameValue(metadata.Title, value))
                                internalTitleCorrectionDetected = true;

                            candidateTitle = value;
                            evidence.Add(
                                $"Internal opening-content evidence identifies Title='{value}'.");
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(explicitUserAuthor))
                {
                    Match openingAuthor =
                        ExplicitOpeningAuthorPattern.Match(openingContent);

                    if (openingAuthor.Success)
                    {
                        string value =
                            CleanIdentityValue(openingAuthor.Groups["value"].Value);

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            if (!SameValue(metadata.Author, value))
                                internalAuthorCorrectionDetected = true;

                            candidateAuthor = value;
                            evidence.Add(
                                $"Internal opening-content evidence identifies Author='{value}'.");
                        }
                    }
                }
            }

            MetadataEvidence? filenameEvidence =
                record.Evidence.FirstOrDefault(
                    item => string.Equals(
                        item.Source,
                        "Filename",
                        StringComparison.OrdinalIgnoreCase));

            bool identityReversalDetected = false;
            bool filenameTitleCorrectionDetected = false;

            if (filenameEvidence != null &&
                !string.IsNullOrWhiteSpace(filenameEvidence.Value))
            {
                string filename = filenameEvidence.Value.Trim();

                string[] parts =
                    filename.Split(
                        " - ",
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries);

                Match explicitSeriesFilenameMatch =
                    ExplicitFilenameSeriesIdentityPattern.Match(filename);

                if (explicitSeriesFilenameMatch.Success)
                {
                    string filenameSeries =
                        explicitSeriesFilenameMatch.Groups["series"].Value.Trim();

                    string filenameSeriesNumber =
                        explicitSeriesFilenameMatch.Groups["number"].Value.Trim();

                    string filenameTitle =
                        explicitSeriesFilenameMatch.Groups["title"].Value.Trim();

                    if (!string.IsNullOrWhiteSpace(filenameTitle))
                    {
                        if (!string.Equals(
                                candidateTitle.Trim(),
                                filenameTitle,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            if (!internalTitleCorrectionDetected &&
                                string.IsNullOrWhiteSpace(explicitUserTitle))
                            {
                                candidateTitle = filenameTitle;
                                filenameTitleCorrectionDetected = true;

                                evidence.Add(
                                    $"Explicit filename series structure identifies Title='{filenameTitle}'.");
                            }
                            else
                            {
                                evidence.Add(
                                    $"Filename Title='{filenameTitle}' differs from the stronger internal Title='{candidateTitle}'; filename retained as corroborating/conflicting evidence.");
                            }
                        }
                        else
                        {
                            evidence.Add(
                                $"Explicit filename series structure corroborates Title='{filenameTitle}'.");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(filenameSeries))
                    {
                        evidence.Add(
                            $"Explicit filename series structure identifies Series='{filenameSeries}'.");
                    }

                    if (!string.IsNullOrWhiteSpace(filenameSeriesNumber))
                    {
                        evidence.Add(
                            $"Explicit filename series structure identifies SeriesNumber='{filenameSeriesNumber}'.");
                    }
                }

                if (parts.Length >= 2)
                {
                    string possibleTitle =
                        RemoveTrailingSeriesPosition(parts[0]);

                    string possibleAuthor =
                        parts[^1].Trim();

                    if (!string.IsNullOrWhiteSpace(possibleTitle) &&
                        !string.IsNullOrWhiteSpace(possibleAuthor) &&
                        LooksLikePersonName(possibleAuthor))
                    {
                        bool authorMatchesFilenameTitle =
                            SameValue(metadata.Author, possibleTitle);

                        bool titleContainsFilenameAuthor =
                            ContainsValue(metadata.Title, possibleAuthor);

                        if (authorMatchesFilenameTitle &&
                            titleContainsFilenameAuthor)
                        {
                            identityReversalDetected = true;

                            if (!internalTitleCorrectionDetected &&
                                string.IsNullOrWhiteSpace(explicitUserTitle))
                            {
                                candidateTitle = possibleTitle;
                            }

                            if (!internalAuthorCorrectionDetected &&
                                string.IsNullOrWhiteSpace(explicitUserAuthor))
                            {
                                candidateAuthor = possibleAuthor;
                            }

                            evidence.Add(
                                $"Filename identity evidence: {filename}");

                            evidence.Add(
                                $"Current metadata: Title='{metadata.Title}', Author='{metadata.Author}'.");

                            evidence.Add(
                                $"Filename identity evidence suggests Title='{possibleTitle}', Author='{possibleAuthor}'.");

                            if (internalTitleCorrectionDetected || internalAuthorCorrectionDetected)
                            {
                                evidence.Add(
                                    $"Internal EPUB identity evidence remains authoritative for any field it explicitly establishes: Title='{candidateTitle}', Author='{candidateAuthor}'.");
                            }

                            IReadOnlyList<string> openingContent =
                                record.Evidence
                                    .Where(item => string.Equals(
                                        item.Source,
                                        "Opening Content",
                                        StringComparison.OrdinalIgnoreCase))
                                    .Select(item => item.Value)
                                    .Where(value => !string.IsNullOrWhiteSpace(value))
                                    .ToList();

                            if (openingContent.Any(text => ContainsValue(text, candidateTitle)))
                            {
                                evidence.Add(
                                    "Opening-content evidence corroborates the candidate title.");
                            }

                            if (openingContent.Any(text => ContainsValue(text, candidateAuthor)))
                            {
                                evidence.Add(
                                    "Opening-content evidence corroborates the candidate author.");
                            }

                            string seriesNumberFromFilename =
                                ExtractSeriesPosition(parts[0]);

                            if (string.IsNullOrWhiteSpace(candidateSeriesNumber) &&
                                !string.IsNullOrWhiteSpace(seriesNumberFromFilename))
                            {
                                candidateSeriesNumber = seriesNumberFromFilename;
                            }

                            string seriesToken =
                                ExtractSeriesToken(parts[0]);

                            if (!string.IsNullOrWhiteSpace(seriesToken))
                            {
                                evidence.Add(
                                    $"Filename contains candidate series token '{seriesToken}'; it is preserved as evidence and not interpreted as a series name.");
                            }
                        }
                    }
                }
            }

            //---------------------------------------------------------
            // Series evidence is evaluated independently of the Title/Author
            // reversal. This allows files such as:
            //     SERRAted Edge #02 - Wheels of Fire
            // to contribute Series evidence even though the final filename
            // segment is not an author's name.
            //---------------------------------------------------------

            if (seriesEvaluation.State == SeriesEvidenceState.Resolved)
            {
                if (!string.IsNullOrWhiteSpace(seriesEvaluation.CandidateSeries))
                    candidateSeries = seriesEvaluation.CandidateSeries;

                if (!string.IsNullOrWhiteSpace(seriesEvaluation.CandidateSeriesNumber))
                    candidateSeriesNumber = seriesEvaluation.CandidateSeriesNumber;
            }

            evidence.AddRange(seriesEvaluation.Evidence);

            bool seriesRepairRequired =
                seriesEvaluation.State == SeriesEvidenceState.Resolved &&
                ((!string.Equals(
                        metadata.Series?.Trim(),
                        candidateSeries.Trim(),
                        StringComparison.OrdinalIgnoreCase) &&
                  !string.IsNullOrWhiteSpace(candidateSeries)) ||
                 (!string.Equals(
                        metadata.SeriesNumber?.Trim(),
                        candidateSeriesNumber.Trim(),
                        StringComparison.OrdinalIgnoreCase) &&
                  !string.IsNullOrWhiteSpace(candidateSeriesNumber)));

            //---------------------------------------------------------
            // If local evidence has identified a real Series question but
            // cannot resolve it, keep the repair branch open. The action layer
            // can then ask for additional evidence rather than silently
            // treating the missing Series as an empty value.
            //---------------------------------------------------------

            bool unresolvedSeriesRequiresAttention =
                seriesEvaluation.State == SeriesEvidenceState.Unresolved ||
                seriesEvaluation.State == SeriesEvidenceState.Conflicting;

            bool explicitUserIdentityRepairRequired =
                !explicitUserIdentityConflict &&
                ((!string.IsNullOrWhiteSpace(explicitUserTitle) &&
                  !string.Equals(
                      metadata.Title?.Trim(),
                      explicitUserTitle.Trim(),
                      StringComparison.OrdinalIgnoreCase)) ||
                 (!string.IsNullOrWhiteSpace(explicitUserAuthor) &&
                  !string.Equals(
                      metadata.Author?.Trim(),
                      explicitUserAuthor.Trim(),
                      StringComparison.OrdinalIgnoreCase)) ||
                 (!string.IsNullOrWhiteSpace(explicitUserSeriesNumber) &&
                  !string.Equals(
                      metadata.SeriesNumber?.Trim(),
                      explicitUserSeriesNumber.Trim(),
                      StringComparison.OrdinalIgnoreCase)));

            bool repairRequired =
                internalTitleCorrectionDetected ||
                internalAuthorCorrectionDetected ||
                identityReversalDetected ||
                filenameTitleCorrectionDetected ||
                seriesRepairRequired ||
                unresolvedSeriesRequiresAttention ||
                explicitUserIdentityRepairRequired ||
                explicitUserIdentityConflict;

            if (!repairRequired)
            {
                return new BookIdentityEvaluation
                {
                    RepairRequired = false,
                    SeriesEvaluation = seriesEvaluation
                };
            }

            BookIdentityCandidate candidate = new()
            {
                Title = candidateTitle,
                Authors = candidateAuthor,
                Series = candidateSeries,
                SeriesNumber = candidateSeriesNumber
            };

            string reason =
                explicitUserIdentityConflict
                    ? "Conflicting explicit user identity information requires clarification."
                    : explicitUserIdentityRepairRequired
                        ? "The user supplied explicit identity information that differs from the observed EPUB metadata."
                        : internalTitleCorrectionDetected || internalAuthorCorrectionDetected
                            ? "The EPUB's own opening content provides stronger Title/Author identity evidence than the observed metadata."
                            : identityReversalDetected
                                ? "The EPUB metadata appears to have Title and Author assigned to the wrong identity fields."
                                : filenameTitleCorrectionDetected
                                    ? "The filename explicitly separates Series, SeriesNumber, and Title, and the filename-derived Title differs from the observed EPUB Title."
                                    : seriesEvaluation.State == SeriesEvidenceState.Resolved
                                ? "Local evidence establishes a Series or SeriesNumber candidate that differs from the observed EPUB metadata."
                                : "Local evidence raises a Series question that requires additional evidence before Scout can safely repair it.";

            BookIdentityEvaluation evaluation = new()
            {
                RepairRequired = true,
                Candidate = candidate,
                SeriesEvaluation = seriesEvaluation,
                Reason = reason
            };

            foreach (string item in evidence.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct())
                evaluation.Evidence.Add(item);

            if (!string.IsNullOrWhiteSpace(candidate.SeriesNumber))
            {
                evaluation.Evidence.Add(
                    $"Series position candidate preserved from local evidence: {candidate.SeriesNumber}.");
            }

            return evaluation;
        }

        private static string? GetSingleExplicitUserValue(
            MetadataRecord record,
            Regex pattern,
            string fieldName)
        {
            List<string> values = GetExplicitUserValues(record, pattern);

            return values.Count == 1
                ? values[0]
                : null;
        }

        private static bool HasConflictingExplicitUserValues(
            MetadataRecord record,
            Regex pattern,
            string fieldName)
        {
            return GetExplicitUserValues(record, pattern).Count > 1;
        }

        private static List<string> GetExplicitUserValues(
            MetadataRecord record,
            Regex pattern)
        {
            return record.Evidence
                .Where(evidence =>
                    string.Equals(
                        evidence.Source,
                        "User",
                        StringComparison.OrdinalIgnoreCase))
                .Select(evidence =>
                    pattern.Match(
                        evidence.Value?.Trim() ?? string.Empty))
                .Where(match => match.Success)
                .Select(match => match.Groups["value"].Value.Trim())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string CleanIdentityValue(string value)
        {
            string cleaned = value.Trim();

            cleaned = cleaned.Trim(" \t\r\n\"'“”‘’.:;,-");

            return Regex.Replace(
                cleaned,
                @"\s+",
                " ").Trim();
        }

        private static string RemoveTrailingSeriesPosition(
            string value)
        {
            return Regex.Replace(
                value.Trim(),
                @"\s+(?:SE\s*)?\d+(?:\.\d+)?\s*$",
                "",
                RegexOptions.IgnoreCase).Trim();
        }

        private static string ExtractSeriesPosition(
            string value)
        {
            Match match = Regex.Match(
                value,
                @"\b(?:SE\s*)?(\d+(?:\.\d+)?)\s*$",
                RegexOptions.IgnoreCase);

            return match.Success
                ? match.Groups[1].Value
                : "";
        }

        private static string ExtractSeriesToken(
            string value)
        {
            Match match = Regex.Match(
                value,
                @"(?:^|\s)([A-Za-z]{2,})\s*\d+(?:\.\d+)?\s*$",
                RegexOptions.IgnoreCase);

            return match.Success
                ? match.Groups[1].Value
                : "";
        }

        private static bool LooksLikePersonName(
            string value)
        {
            string[] parts =
                value.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

            if (parts.Length < 2 || parts.Length > 5)
                return false;

            return parts.All(part => part.Any(char.IsLetter));
        }

        private static bool SameValue(
            string? left,
            string? right)
        {
            return Normalize(left) == Normalize(right);
        }

        private static bool ContainsValue(
            string? value,
            string? candidate)
        {
            string[] valueTokens =
                Normalize(value)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            string[] candidateTokens =
                Normalize(candidate)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return candidateTokens.Length > 0 &&
                   candidateTokens.All(
                       token => valueTokens.Contains(
                           token,
                           StringComparer.Ordinal));
        }

        private static string Normalize(
            string? value)
        {
            return Regex.Replace(
                    value ?? "",
                    @"[^a-z0-9]+",
                    " ",
                    RegexOptions.IgnoreCase)
                .Trim()
                .ToLowerInvariant();
        }
    }
}
