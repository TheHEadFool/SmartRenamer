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

        // Opening-page text is useful only when it explicitly labels a series.
        // Anchoring to a line avoids treating ordinary narrative mentions of a
        // series as metadata. Position evidence is accepted only alongside an
        // explicit series label in the same opening-content evidence block.
        private static readonly Regex ExplicitOpeningSeriesPattern =
            new(
                @"(?im)(?:^|[|;\r\n])\s*(?:series|series title|book series|belongs to collection)\s*(?:is|=|:)\s*(?<series>[^|;\r\n]{2,100}?)\s*(?=(?:[|;\r\n]|$|\s+(?:identifiers|subjects|notes|edition|other\s+titles|classification)\s*:))",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex ExplicitOpeningSeriesNumberPattern =
            new(
                @"(?im)(?:^|[|;\r\n])\s*(?:series number|series position|position in series)\s*(?:is|=|:)\s*(?<number>\d+(?:\.\d+)?)\s*(?=[|;\r\n]|$)",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

                if ((string.Equals(evidence.Source, "EPUB Metadata", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(evidence.Source, "Front Matter Consultant", StringComparison.OrdinalIgnoreCase)) &&
                    string.Equals(evidence.Field, "SeriesNumber", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(evidence.Value))
                {
                    string numberSeries = string.Equals(evidence.Source, "EPUB Metadata", StringComparison.OrdinalIgnoreCase)
                        ? (record.Metadata.Series ?? string.Empty)
                        : (record.Evidence.FirstOrDefault(e =>
                            string.Equals(e.Source, "Front Matter Consultant", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(e.Field, "Series", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(e.Location, evidence.Location, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty);
                    if (IsValidSeriesPosition(evidence.Value.Trim()))
                        numberSignals.Add(new SeriesNumberSignal(evidence.Value.Trim(), evidence.Source, numberSeries));
                }

                //---------------------------------------------------------
                // Collection and source-folder evidence are contextual.
                // They are useful corroboration, not automatic truth.
                //---------------------------------------------------------

                if (string.Equals(
                        evidence.Field,
                        "Series",
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(evidence.Value))
                {
                    bool isCollectionEvidence = string.Equals(
                        evidence.Source,
                        "Collection",
                        StringComparison.OrdinalIgnoreCase);
                    bool isSourceFolderEvidence = string.Equals(
                        evidence.Source,
                        "Source Folder",
                        StringComparison.OrdinalIgnoreCase);
                    bool isExplicitTitleEvidence = string.Equals(
                        evidence.Source,
                        "Title",
                        StringComparison.OrdinalIgnoreCase);
                    bool isExplicitOpeningSeriesEvidence =
                        (string.Equals(evidence.Source, "Opening Content", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(evidence.Source, "Front Matter Consultant", StringComparison.OrdinalIgnoreCase)) &&
                        string.Equals(evidence.Field, "Series", StringComparison.OrdinalIgnoreCase);

                    // The selected input folder is context, not automatically
                    // a series name. A generic folder such as "1", "test",
                    // or an author-named folder must not create an unresolved
                    // Series repair for every book in the collection. Keep
                    // meaningful series-named folders available as evidence.
                    if (isCollectionEvidence ||
                        isExplicitTitleEvidence ||
                        isExplicitOpeningSeriesEvidence ||
                        (isSourceFolderEvidence &&
                         IsMeaningfulSourceFolderSeriesHint(
                             evidence.Value.Trim(), record)))
                    {
                        signals.Add(
                            new SeriesSignal(
                                evidence.Value.Trim(),
                                evidence.Source));
                    }
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
                                "Filename",
                                match.Groups["series"].Value.Trim()));
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

                //---------------------------------------------------------
                // Opening-content evidence can contribute series identity
                // and position only through explicit, labelled statements.
                // We never derive a number from collection order or arbitrary
                // numbers in prose. A position is retained only when the same
                // content block explicitly identifies the series.
                //---------------------------------------------------------

                if (string.Equals(
                        evidence.Source,
                        "Opening Content",
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        evidence.Field,
                        "Content",
                        StringComparison.OrdinalIgnoreCase))
                {
                    string content = evidence.Value ?? string.Empty;
                    Match seriesMatch = ExplicitOpeningSeriesPattern.Match(content);
                    Match positionMatch = ExplicitOpeningSeriesNumberPattern.Match(content);

                    if (seriesMatch.Success)
                    {
                        string openingSeries = CleanExplicitSeriesValue(
                            seriesMatch.Groups["series"].Value);

                        if (!string.IsNullOrWhiteSpace(openingSeries))
                        {
                            signals.Add(new SeriesSignal(openingSeries, "Opening Content"));

                            if (positionMatch.Success)
                            {
                                string openingPosition = positionMatch.Groups["number"].Value.Trim();
                                if (IsValidSeriesPosition(openingPosition))
                                    numberSignals.Add(new SeriesNumberSignal(openingPosition, "Opening Content", openingSeries));
                            }
                        }
                    }
                }
            }

            // Some EPUBs encode the series position in dc:title even when
            // the structured SeriesNumber field is absent. Interpret that
            // title as position evidence only when its series portion agrees
            // with the EPUB's own Series observation. This is evidence, not
            // a rule for manufacturing a number from collection order.
            if (!string.IsNullOrWhiteSpace(record.Metadata.Title))
            {
                Match titleMatch = ExplicitFilenameSeriesPattern.Match(
                    record.Metadata.Title.Trim());

                if (titleMatch.Success &&
                    signals.Any(signal =>
                        string.Equals(
                            signal.Source,
                            "EPUB Metadata",
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            NormalizeSeriesKey(signal.Value),
                            NormalizeSeriesKey(
                                titleMatch.Groups["series"].Value.Trim()),
                            StringComparison.OrdinalIgnoreCase)))
                {
                    numberSignals.Add(
                        new SeriesNumberSignal(
                            titleMatch.Groups["number"].Value.Trim(),
                            "EPUB Metadata Title",
                            titleMatch.Groups["series"].Value.Trim()));
                }
            }

            //---------------------------------------------------------
            // If the EPUB already contains one consistent Series value,
            // preserve that observed value. The evaluator does not replace
            // explicit EPUB metadata with contextual evidence.
            //---------------------------------------------------------

            // Structured OPF series metadata is direct evidence. A parsed
            // Library-of-Congress catalog statement inside the EPUB is also an
            // explicit bibliographic declaration, not a weak contextual clue;
            // it can establish series identity on its own, but never a series
            // number. Keep ordinary front-matter clues subject to corroboration.
            bool IsDirectBibliographicSeriesSignal(SeriesSignal signal) =>
                string.Equals(
                    signal.Source,
                    "EPUB Metadata",
                    StringComparison.OrdinalIgnoreCase) ||
                (string.Equals(
                    signal.Source,
                    "Front Matter Consultant",
                    StringComparison.OrdinalIgnoreCase) &&
                 record.Evidence.Any(evidence =>
                    string.Equals(evidence.Source, signal.Source, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(evidence.Field, "Series", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(evidence.Value, signal.Value, StringComparison.OrdinalIgnoreCase) &&
                    evidence.Notes.Contains(
                        "Library of Congress-style title/creator catalog statement",
                        StringComparison.OrdinalIgnoreCase)));

            List<SeriesSignal> directSignals =
                signals
                    .Where(IsDirectBibliographicSeriesSignal)
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
                            !IsDirectBibliographicSeriesSignal(signal) &&
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
                    ResolveSeriesNumber(numberSignals, record.Metadata.SeriesNumber, canonicalSeries),
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
                    ResolveSeriesNumber(numberSignals, record.Metadata.SeriesNumber, candidate),
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
            string observed,
            string resolvedSeries)
        {
            // Preserve an existing embedded position as observed data. For a
            // new candidate, however, require the number clue to be tied to the
            // same resolved series identity. This prevents a number extracted
            // from one title/series from leaking into another candidate.
            if (!string.IsNullOrWhiteSpace(observed))
                return observed.Trim();

            List<string> values =
                signals
                    .Where(signal =>
                        !string.IsNullOrWhiteSpace(signal.SeriesKey) &&
                        string.Equals(
                            NormalizeSeriesKey(signal.SeriesKey),
                            NormalizeSeriesKey(resolvedSeries),
                            StringComparison.OrdinalIgnoreCase))
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

        private static string CleanExplicitSeriesValue(string value)
        {
            string cleaned = Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
            cleaned = cleaned.Trim(' ', '\t', '.', ':', ';', '-', '–', '—', '"', '\'');
            if (cleaned.Length < 3 || cleaned.Length > 100)
                return string.Empty;

            // Avoid accepting page furniture or production credits as a series.
            if (Regex.IsMatch(cleaned,
                @"^(?:cover|jacket|interior|copyright|title page|table of contents|publisher|isbn|written by|by)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                return string.Empty;

            return cleaned;
        }

        private static bool IsValidSeriesPosition(string value)
        {
            if (!Regex.IsMatch(value ?? string.Empty, @"^\d+(?:\.\d+)?$"))
                return false;

            return decimal.TryParse(
                value,
                System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal position) && position >= 0;
        }

        private static bool IsMeaningfulSourceFolderSeriesHint(
            string folderName,
            MetadataRecord record)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                return false;

            string trimmed = folderName.Trim();
            string normalized = NormalizeSeriesKey(trimmed);

            if (string.IsNullOrWhiteSpace(normalized) ||
                normalized.All(char.IsDigit))
            {
                return false;
            }

            // These are common container/source-folder labels, not useful
            // evidence that a book belongs to a series.
            string[] genericFolderNames =
            {
                "test", "tests", "books", "ebooks", "e books", "library",
                "downloads", "download", "documents", "desktop", "new folder",
                "untitled", "collection", "misc", "miscellaneous", "unsorted",
                "to sort", "to organize", "organized", "output", "input"
            };

            if (genericFolderNames.Any(name =>
                    string.Equals(
                        normalized,
                        NormalizeSeriesKey(name),
                        StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            // Author folders are useful organization labels, but are not
            // series names. Compare the folder against the observed author
            // value without changing the metadata or guessing at a series.
            string observedAuthor = record.Metadata?.Author?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(observedAuthor) &&
                string.Equals(
                    normalized,
                    NormalizeSeriesKey(observedAuthor),
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
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
            string Source,
            string SeriesKey = "");
    }

}
