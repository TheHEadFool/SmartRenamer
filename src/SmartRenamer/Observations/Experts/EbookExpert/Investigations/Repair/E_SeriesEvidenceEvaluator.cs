using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Scout.Observations.Experts.EbookExpert.Data;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// Evaluates Series and SeriesNumber evidence already collected by the
    /// Ebook Expert.
    ///
    /// This evaluator deliberately does not perform external research, assign
    /// arbitrary confidence scores, or modify an EPUB. It only determines
    /// whether the existing evidence converges strongly enough to establish a
    /// local candidate.
    /// </summary>
    internal sealed class E_SeriesEvidenceEvaluator
    {
        private static readonly Regex ExplicitFilenameSeriesPattern =
            new(
                @"^(?:(?<collectionNumber>\d+(?:\.\d+)?)\s*-\s*)?(?<series>.+?)\s+#\s*(?<number>\d+(?:\.\d+)?)\s+-\s+(?<title>.+)$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ExplicitUserSeriesPattern =
            new(
                @"\b(?:series\s*(?:is|=|:)|part\s+of\s+(?:the\s+)?)\s*(?<series>[^.;,\r\n]+)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public SeriesEvidenceEvaluation Evaluate(
            MetadataRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);

            List<SeriesSignal> signals = new();
            List<SeriesNumberSignal> numberSignals = new();

            //---------------------------------------------------------
            // Direct EPUB metadata is the strongest local observation.
            //---------------------------------------------------------

            foreach (MetadataEvidence evidence in record.Evidence)
            {
                if (string.Equals(
                        evidence.Source,
                        "EPUB Metadata",
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        evidence.Field,
                        "Series",
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(evidence.Value))
                {
                    signals.Add(
                        new SeriesSignal(
                            evidence.Value.Trim(),
                            "EPUB Metadata"));
                }

                if (string.Equals(
                        evidence.Source,
                        "EPUB Metadata",
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        evidence.Field,
                        "SeriesNumber",
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(evidence.Value))
                {
                    numberSignals.Add(
                        new SeriesNumberSignal(
                            evidence.Value.Trim(),
                            "EPUB Metadata"));
                }

                //---------------------------------------------------------
                // Collection and source-folder evidence are contextual.
                // They are useful corroboration, not automatic truth.
                //---------------------------------------------------------

                if ((string.Equals(
                        evidence.Source,
                        "Collection",
                        StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(
                        evidence.Source,
                        "Source Folder",
                        StringComparison.OrdinalIgnoreCase)) &&
                    string.Equals(
                        evidence.Field,
                        "Series",
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(evidence.Value))
                {
                    signals.Add(
                        new SeriesSignal(
                            evidence.Value.Trim(),
                            evidence.Source));
                }

                //---------------------------------------------------------
                // A filename of the form:
                //     Series Name #02 - Book Title
                // explicitly carries both series and position evidence.
                //---------------------------------------------------------

                if (string.Equals(
                        evidence.Source,
                        "Filename",
                        StringComparison.OrdinalIgnoreCase))
                {
                    Match match = ExplicitFilenameSeriesPattern.Match(
                        evidence.Value?.Trim() ?? string.Empty);

                    if (match.Success)
                    {
                        signals.Add(
                            new SeriesSignal(
                                match.Groups["series"].Value.Trim(),
                                "Filename"));

                        numberSignals.Add(
                            new SeriesNumberSignal(
                                match.Groups["number"].Value.Trim(),
                                "Filename"));
                    }
                }

                //---------------------------------------------------------
                // Explicit user statements may add a series candidate.
                // Free-form user evidence that does not identify a series
                // remains evidence for later research rather than being
                // guessed into metadata.
                //---------------------------------------------------------

                if (string.Equals(
                        evidence.Source,
                        "User",
                        StringComparison.OrdinalIgnoreCase))
                {
                    Match match = ExplicitUserSeriesPattern.Match(
                        evidence.Value?.Trim() ?? string.Empty);

                    if (match.Success)
                    {
                        string value = match.Groups["series"].Value.Trim();

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            signals.Add(
                                new SeriesSignal(
                                    value,
                                    "User"));
                        }
                    }
                }
            }

            //---------------------------------------------------------
            // If the EPUB already contains one consistent Series value,
            // preserve that observed value. The evaluator does not replace
            // explicit EPUB metadata with contextual evidence.
            //---------------------------------------------------------

            List<SeriesSignal> directSignals =
                signals
                    .Where(signal =>
                        string.Equals(
                            signal.Source,
                            "EPUB Metadata",
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

            List<string> directValues =
                directSignals
                    .Select(signal => signal.Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            if (directValues.Count == 1)
            {
                string observedSeries = directValues[0];
                string canonicalSeries = observedSeries;

                // When another source independently corroborates the same
                // normalized identity, prefer the collection's concrete
                // spelling for the repair candidate. This allows display
                // variants such as "SERRAted Edge Series" and "SERRAted Edge"
                // to converge without hard-coding either title.
                SeriesSignal? contextualCandidate =
                    signals
                        .Where(signal =>
                            !string.Equals(
                                signal.Source,
                                "EPUB Metadata",
                                StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(
                                NormalizeSeriesKey(signal.Value),
                                NormalizeSeriesKey(observedSeries),
                                StringComparison.OrdinalIgnoreCase))
                        .OrderBy(signal => SeriesSourceRank(signal.Source))
                        .FirstOrDefault();

                bool contextualAgreement = contextualCandidate != null;
                bool compositeContextualAgreement = false;

                if (contextualCandidate == null)
                {
                    contextualCandidate =
                        FindCompositeSeriesContextualCandidate(
                            observedSeries,
                            signals);

                    compositeContextualAgreement =
                        contextualCandidate != null;
                }

                if (contextualCandidate != null)
                    canonicalSeries = contextualCandidate.Value;

                string reason = contextualAgreement
                    ? "The EPUB contains a Series value and contextual evidence independently agrees with its normalized identity; the corroborated spelling is the repair candidate."
                    : compositeContextualAgreement
                        ? "The EPUB contains structured Series metadata whose series-position suffix is corroborated by independent collection or source-folder evidence; the contextual series identity is the repair candidate."
                        : "The EPUB contains an explicit Series value.";

                return BuildResolved(
                    canonicalSeries,
                    ResolveSeriesNumber(numberSignals, record.Metadata.SeriesNumber),
                    signals,
                    numberSignals,
                    reason);
            }

            if (directValues.Count > 1)
            {
                return BuildConflicting(
                    signals,
                    numberSignals,
                    "The EPUB contains conflicting Series observations.");
            }

            //---------------------------------------------------------
            // Group non-direct candidates by a conservative identity key.
            // Leading 'The' and trailing 'Series' are treated as display
            // wrappers only for comparison; the original observed spelling
            // remains the candidate value.
            //---------------------------------------------------------

            var groups =
                signals
                    .GroupBy(
                        signal => NormalizeSeriesKey(signal.Value),
                        StringComparer.OrdinalIgnoreCase)
                    .Where(group => !string.IsNullOrWhiteSpace(group.Key))
                    .ToList();

            if (groups.Count == 0)
            {
                return new SeriesEvidenceEvaluation
                {
                    State = SeriesEvidenceState.NoSignal,
                    Reason = "No local evidence currently establishes a Series value."
                };
            }

            var rankedGroups =
                groups
                    .Select(group => new
                    {
                        Group = group,
                        SourceCount = group
                            .Select(signal => signal.Source)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .Count()
                    })
                    .OrderByDescending(item => item.SourceCount)
                    .ToList();

            var strongest = rankedGroups[0];

            if (rankedGroups.Count > 1 &&
                strongest.SourceCount == rankedGroups[1].SourceCount &&
                strongest.SourceCount >= 2)
            {
                return BuildConflicting(
                    signals,
                    numberSignals,
                    "Local Series evidence supports more than one independently corroborated candidate.");
            }

            //---------------------------------------------------------
            // Two or more distinct evidence sources agreeing is enough
            // for a local candidate. One contextual clue alone is not.
            //---------------------------------------------------------

            if (strongest.SourceCount >= 2)
            {
                string candidate =
                    strongest.Group
                        .OrderBy(signal => SeriesSourceRank(signal.Source))
                        .Select(signal => signal.Value)
                        .First();

                return BuildResolved(
                    candidate,
                    ResolveSeriesNumber(numberSignals, record.Metadata.SeriesNumber),
                    signals,
                    numberSignals,
                    "Independent local evidence sources converge on the same Series candidate.");
            }

            SeriesEvidenceEvaluation unresolved = new()
            {
                State = SeriesEvidenceState.Unresolved,
                Reason = "Local evidence suggests a Series value, but it is not yet corroborated by another source."
            };

            AddEvidence(unresolved, signals, numberSignals);
            return unresolved;
        }

        private static SeriesEvidenceEvaluation BuildResolved(
            string series,
            string seriesNumber,
            IReadOnlyList<SeriesSignal> signals,
            IReadOnlyList<SeriesNumberSignal> numberSignals,
            string reason)
        {
            SeriesEvidenceEvaluation result = new()
            {
                State = SeriesEvidenceState.Resolved,
                CandidateSeries = series,
                CandidateSeriesNumber = seriesNumber,
                Reason = reason
            };

            AddEvidence(result, signals, numberSignals);
            return result;
        }

        private static SeriesEvidenceEvaluation BuildConflicting(
            IReadOnlyList<SeriesSignal> signals,
            IReadOnlyList<SeriesNumberSignal> numberSignals,
            string reason)
        {
            SeriesEvidenceEvaluation result = new()
            {
                State = SeriesEvidenceState.Conflicting,
                Reason = reason
            };

            AddEvidence(result, signals, numberSignals);
            return result;
        }

        private static void AddEvidence(
            SeriesEvidenceEvaluation result,
            IReadOnlyList<SeriesSignal> signals,
            IReadOnlyList<SeriesNumberSignal> numberSignals)
        {
            foreach (SeriesSignal signal in signals)
            {
                result.Evidence.Add(
                    $"Series evidence ({signal.Source}): {signal.Value}");
            }

            foreach (SeriesNumberSignal signal in numberSignals)
            {
                result.Evidence.Add(
                    $"Series position evidence ({signal.Source}): {signal.Value}");
            }
        }

        private static string ResolveSeriesNumber(
            IReadOnlyList<SeriesNumberSignal> signals,
            string observed)
        {
            if (!string.IsNullOrWhiteSpace(observed))
                return observed.Trim();

            List<string> values =
                signals
                    .Select(signal => signal.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            return values.Count == 1
                ? values[0]
                : "";
        }

        private static SeriesSignal? FindCompositeSeriesContextualCandidate(
            string observedSeries,
            IReadOnlyList<SeriesSignal> signals)
        {
            string seriesText = observedSeries ?? string.Empty;

            if (string.IsNullOrWhiteSpace(seriesText))
                return null;

            Match numberMatch = Regex.Match(
                seriesText,
                @"\b\d+(?:\.\d+)?\b",
                RegexOptions.IgnoreCase);

            if (!numberMatch.Success)
                return null;

            string suffix =
                seriesText[(numberMatch.Index + numberMatch.Length)..];

            string[] suffixTokens = GetMeaningfulSeriesTokens(suffix);

            if (suffixTokens.Length == 0)
                return null;

            SeriesSignal? best = null;
            int bestMatches = 0;

            foreach (SeriesSignal signal in signals)
            {
                if (string.Equals(
                        signal.Source,
                        "EPUB Metadata",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.Equals(signal.Source, "Source Folder", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(signal.Source, "Collection", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string[] contextualTokens =
                    GetMeaningfulSeriesTokens(signal.Value);

                int matches = suffixTokens.Count(
                    token => contextualTokens.Any(
                        candidate => string.Equals(
                            candidate,
                            token,
                            StringComparison.OrdinalIgnoreCase)));

                if (matches > bestMatches)
                {
                    bestMatches = matches;
                    best = signal;
                }
            }

            // One shared suffix token is not enough by itself to establish a
            // series. It becomes useful here only when the direct EPUB value
            // has the characteristic structured form of a broader prefix, a
            // numeric position, and a trailing series token, and the same
            // token independently occurs in the collection/source-folder
            // identity. The evidence remains local and auditable.
            return bestMatches >= 1
                ? best
                : null;
        }

        private static string[] GetMeaningfulSeriesTokens(string value)
        {
            return Regex
                .Split(
                    value ?? string.Empty,
                    @"[^A-Za-z0-9]+")
                .Where(token =>
                    !string.IsNullOrWhiteSpace(token) &&
                    !token.All(char.IsDigit) &&
                    !string.Equals(token, "the", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(token, "series", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        private static int SeriesSourceRank(string source)
        {
            return source switch
            {
                "EPUB Metadata" => 0,
                "Source Folder" => 1,
                "Collection" => 2,
                "User" => 3,
                "Filename" => 4,
                _ => 5
            };
        }

        private static string NormalizeSeriesKey(string value)
        {
            string normalized =
                Regex.Replace(
                    value ?? string.Empty,
                    @"[^A-Za-z0-9]+",
                    " ")
                .Trim()
                .ToLowerInvariant();

            if (normalized.StartsWith("the ", StringComparison.Ordinal))
                normalized = normalized[4..].Trim();

            if (normalized.EndsWith(" series", StringComparison.Ordinal))
                normalized = normalized[..^7].Trim();

            return normalized;
        }

        private sealed record SeriesSignal(
            string Value,
            string Source);

        private sealed record SeriesNumberSignal(
            string Value,
            string Source);
    }

}
