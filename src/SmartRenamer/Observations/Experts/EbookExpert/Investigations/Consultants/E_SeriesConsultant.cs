using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Scout.Observations.Experts.EbookExpert.Data;
using SmartRenamer.Models;
using SmartRenamer.Observations;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants
{
    /// <summary>
    /// Reviews series evidence across the whole ebook collection.
    /// This consultant recommends; it never writes metadata, chooses a series
    /// number, or changes organization paths.
    /// </summary>
    internal sealed class E_SeriesConsultant
    {
        private static readonly Regex NovellaOrStoryPattern = new(
            @"\b(?:a|an)\s+(?<series>[^:()]+?)\s+(?:novel|novella|short\s+story|story)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex StoriesFromPattern = new(
            @"\b(?:stories|tales|short\s+stories)\s+from\s+(?:the\s+)?(?<series>[^:()]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ParentheticalSeriesPattern = new(
            @"\((?:the\s+)?(?<series>[A-Z][^()]{2,60})\)",
            RegexOptions.Compiled);

        private static readonly Regex ExplicitOpeningSeriesPattern = new(
            @"(?im)(?:^|[|;\r\n])\s*(?:series|series title|book series|belongs to collection)\s*(?:is|=|:)\s*(?<series>[^|;\r\n]{2,100}?)\s*(?=(?:[|;\r\n]|$|\s+(?:identifiers|subjects|notes|edition|other\s+titles|classification)\s*:))",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Adds corroborated collection-level Series evidence to the records
        /// before the Repair Investigation builds its opportunity queue.
        /// This is evidence-only: observed metadata and SeriesNumber are never
        /// changed. A book must itself carry a matching series signal, and the
        /// normalized series identity must be supported by at least two books.
        /// </summary>
        public int ContributeCorroboratedEvidence(MetadataReport report)
        {
            ArgumentNullException.ThrowIfNull(report);

            List<BookSeriesEvidence> books = report.Records
                .Where(record => record != null && record.Metadata != null)
                .Select(BuildBookEvidence)
                .Where(book => book.Signals.Count > 0)
                .ToList();

            int added = 0;

            // Preserve explicit per-book clues as evidence records before the
            // collection cluster is contributed. Without this step the
            // consultant can see a title such as "The Law: A Dresden Files
            // Novella" while the repair evaluator sees only one Collection
            // signal and therefore cannot resolve the series identity.
            foreach (BookSeriesEvidence book in books)
            {
                MetadataRecord? record = report.Records.FirstOrDefault(candidate =>
                    string.Equals(
                        candidate.File?.OriginalFullPath ?? candidate.File?.CurrentFullPath,
                        book.Path,
                        StringComparison.OrdinalIgnoreCase));
                if (record == null)
                    continue;

                foreach (SeriesSignal signal in book.Signals.Where(signal =>
                    string.Equals(signal.Source, "Title subtitle clue", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(signal.Source, "Title collection clue", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(signal.Source, "Corroborating title parenthetical", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(signal.Source, "Opening Content", StringComparison.OrdinalIgnoreCase)))
                {
                    string evidenceSource = string.Equals(
                        signal.Source, "Opening Content", StringComparison.OrdinalIgnoreCase)
                        ? "Opening Content"
                        : "Title";
                    bool alreadyPresent = record.Evidence.Any(evidence =>
                        string.Equals(evidence.Source, evidenceSource, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(evidence.Field, "Series", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(NormalizeKey(evidence.Value), NormalizeKey(signal.Value), StringComparison.OrdinalIgnoreCase));
                    if (alreadyPresent)
                        continue;

                    report.AddEvidence(record, new MetadataEvidence
                    {
                        Source = evidenceSource,
                        Field = "Series",
                        Value = signal.Value,
                        Location = evidenceSource == "Title"
                            ? "Explicit series clue in the EPUB title"
                            : "Explicitly labelled series clue in EPUB opening content",
                        Notes = $"Series Consultant extracted an explicit clue ({signal.Source}). This is evidence only; it does not infer a series number or modify embedded metadata."
                    });
                    added++;
                }
            }

            var clusters = books
                .SelectMany(book => book.Signals
                    .GroupBy(signal => NormalizeKey(signal.Value), StringComparer.OrdinalIgnoreCase)
                    .Select(group => new
                    {
                        Book = book,
                        Key = group.Key,
                        Signal = group.OrderByDescending(signal => signal.Rank).First()
                    }))
                .Where(item => !string.IsNullOrWhiteSpace(item.Key))
                .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var cluster in clusters)
            {
                var distinctBooks = cluster
                    .GroupBy(item => item.Book.Path, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.OrderByDescending(item => item.Signal.Rank).First())
                    .ToList();

                if (distinctBooks.Count < 2)
                    continue;

                string canonical = cluster
                    .GroupBy(item => item.Signal.Value.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(group => new
                    {
                        Label = group.Key,
                        Rank = group.Max(item => item.Signal.Rank),
                        Count = group.Select(item => item.Book.Path)
                            .Distinct(StringComparer.OrdinalIgnoreCase).Count()
                    })
                    .OrderByDescending(item => item.Rank)
                    .ThenByDescending(item => item.Count)
                    .ThenBy(item => item.Label.Length)
                    .Select(item => item.Label)
                    .First();

                foreach (var item in distinctBooks)
                {
                    BookSeriesEvidence book = item.Book;
                    MetadataRecord? record = report.Records.FirstOrDefault(candidate =>
                        string.Equals(
                            candidate.File?.OriginalFullPath ?? candidate.File?.CurrentFullPath,
                            book.Path,
                            StringComparison.OrdinalIgnoreCase));

                    if (record == null)
                        continue;

                    // Do not use a collection majority to overwrite or bypass
                    // conflicting direct series evidence on the same book.
                    var bookKeys = book.Signals
                        .Select(signal => NormalizeKey(signal.Value))
                        .Where(key => !string.IsNullOrWhiteSpace(key))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    if (bookKeys.Count != 1 ||
                        !string.Equals(bookKeys[0], cluster.Key, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!string.IsNullOrWhiteSpace(book.ObservedSeries))
                        continue;

                    bool alreadyPresent = record.Evidence.Any(evidence =>
                        string.Equals(evidence.Source, "Collection", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(evidence.Field, "Series", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(NormalizeKey(evidence.Value), cluster.Key, StringComparison.OrdinalIgnoreCase));
                    if (alreadyPresent)
                        continue;

                    report.AddEvidence(record, new MetadataEvidence
                    {
                        Source = "Collection",
                        Field = "Series",
                        Value = canonical,
                        Location = "EbookExpert collection-wide Series Consultant",
                        Notes = $"Corroborated by {distinctBooks.Count} distinct books. Candidate evidence only; observed EPUB metadata was not changed and no SeriesNumber was inferred."
                    });
                    added++;
                }
            }

            Debug.WriteLine(
                $"[SERIES CONSULTANT] EVIDENCE CONTRIBUTION COMPLETE | BooksWithSignals={books.Count} | " +
                $"EvidenceAdded={added} | MetadataWritten=False | SeriesNumbersInvented=False");
            return added;
        }

        public List<ExpertFinding> Review(MetadataReport report)
        {
            ArgumentNullException.ThrowIfNull(report);

            List<BookSeriesEvidence> books = report.Records
                .Where(record => record != null && record.Metadata != null)
                .Select(BuildBookEvidence)
                .Where(book => book.Signals.Count > 0)
                .ToList();

            if (books.Count == 0)
            {
                Debug.WriteLine("[SERIES CONSULTANT] No explicit or corroborating series evidence found.");
                return new List<ExpertFinding>();
            }

            List<ExpertFinding> findings = new();

            // A disagreement within one book is a real conflict. Do not let a
            // collection majority silently override the book's direct metadata.
            foreach (BookSeriesEvidence book in books)
            {
                List<SeriesSignal> distinct = book.Signals
                    .GroupBy(signal => NormalizeKey(signal.Value), StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .ToList();

                if (distinct.Count > 1)
                {
                    ExpertFinding conflict = new()
                    {
                        FoundSomething = true,
                        ContextId = book.Path,
                        Confidence = 0.0,
                        Summary = $"Series evidence conflicts for '{book.Title}'. Scout will not choose a series automatically."
                    };
                    foreach (SeriesSignal signal in book.Signals)
                        conflict.Evidence.Add($"{signal.Source}: '{signal.Value}'.");
                    findings.Add(conflict);
                }
            }

            // Evaluate normalized series identities collection-wide. Removing a
            // leading article and harmless punctuation is only a grouping key;
            // the canonical display label is selected from the strongest actual
            // evidence and is shown as a recommendation, not written to a book.
            var clusters = books
                .SelectMany(book => book.Signals
                    .GroupBy(signal => NormalizeKey(signal.Value), StringComparer.OrdinalIgnoreCase)
                    .Select(group => new { Book = book, Key = group.Key, Signal = group
                        .OrderByDescending(signal => signal.Rank)
                        .First() }))
                .Where(item => !string.IsNullOrWhiteSpace(item.Key))
                .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var cluster in clusters)
            {
                var distinctBooks = cluster
                    .GroupBy(item => item.Book.Path, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.OrderByDescending(item => item.Signal.Rank).First())
                    .ToList();

                // One weak title clue alone is not enough to claim a collection
                // series. Require at least two separate books to corroborate it.
                if (distinctBooks.Count < 2)
                    continue;

                string canonical = cluster
                    .GroupBy(item => item.Signal.Value.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(group => new
                    {
                        Label = group.Key,
                        Count = group.Select(item => item.Book.Path)
                            .Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                        Rank = group.Max(item => item.Signal.Rank)
                    })
                    .OrderByDescending(item => item.Rank)
                    .ThenByDescending(item => item.Count)
                    .ThenBy(item => item.Label.Length)
                    .Select(item => item.Label)
                    .First();

                List<string> variants = cluster
                    .Select(item => item.Signal.Value.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                List<BookSeriesEvidence> booksInCluster = distinctBooks
                    .Select(item => item.Book)
                    .DistinctBy(book => book.Path, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                List<BookSeriesEvidence> needingSeries = booksInCluster
                    .Where(book => string.IsNullOrWhiteSpace(book.ObservedSeries) ||
                        !string.Equals(NormalizeKey(book.ObservedSeries), cluster.Key, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                ExpertFinding finding = new()
                {
                    FoundSomething = true,
                    Confidence = 0.75,
                    Summary = variants.Count > 1
                        ? $"The collection supports one series identity across {booksInCluster.Count} books, but its labels vary. Recommend '{canonical}' as the canonical label for review."
                        : $"Series '{canonical}' is corroborated by evidence from {booksInCluster.Count} books. Review it as a collection-wide series candidate."
                };

                finding.Evidence.Add($"Distinct books supporting this series identity: {booksInCluster.Count}.");
                if (variants.Count > 1)
                    finding.Evidence.Add("Observed label variants: " + string.Join("; ", variants) + ".");
                if (needingSeries.Count > 0)
                    finding.Evidence.Add($"{needingSeries.Count} book(s) have a missing or non-matching observed Series value; no metadata was changed.");

                foreach (var item in distinctBooks.OrderBy(item => item.Book.Title, StringComparer.OrdinalIgnoreCase))
                {
                    finding.Evidence.Add(
                        $"{item.Book.Title}: '{item.Signal.Value}' ({item.Signal.Source}).");
                }

                findings.Add(finding);
                Debug.WriteLine(
                    $"[SERIES CONSULTANT] CLUSTER | Key='{cluster.Key}' | Canonical='{canonical}' " +
                    $"| Books={booksInCluster.Count} | Variants={variants.Count} | RecommendationsOnly=True");
            }

            Debug.WriteLine(
                $"[SERIES CONSULTANT] COMPLETE | BooksWithSignals={books.Count} | Findings={findings.Count} | MetadataWritten=False | SeriesNumbersInvented=False");
            return findings;
        }

        private static BookSeriesEvidence BuildBookEvidence(MetadataRecord record)
        {
            string title = record.Metadata.Title?.Trim() ?? string.Empty;
            string path = record.File?.OriginalFullPath ?? record.File?.CurrentFullPath ?? title;
            BookSeriesEvidence book = new()
            {
                Path = path,
                Title = string.IsNullOrWhiteSpace(title) ? System.IO.Path.GetFileName(path) : title,
                ObservedSeries = record.Metadata.Series?.Trim() ?? string.Empty
            };

            AddSignal(book, record.Metadata.Series, "EPUB Series metadata", 100);

            foreach (MetadataEvidence evidence in record.Evidence)
            {
                if (string.Equals(evidence.Field, "Series", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(evidence.Source, "Source Folder", StringComparison.OrdinalIgnoreCase) &&
                        !IsMeaningfulFolderSeriesHint(evidence.Value ?? string.Empty, record))
                        continue;

                    int rank = SourceRank(evidence.Source);
                    AddSignal(book, evidence.Value, evidence.Source, rank);
                }

                // Opening-page evidence is used only when the text explicitly
                // labels the series. Ordinary mentions in narrative prose are
                // not collection signals. The evaluator independently validates
                // the candidate before it can become a repair.
                if (string.Equals(evidence.Source, "Opening Content", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(evidence.Field, "Content", StringComparison.OrdinalIgnoreCase))
                {
                    Match openingSeries = ExplicitOpeningSeriesPattern.Match(evidence.Value ?? string.Empty);
                    if (openingSeries.Success)
                    {
                        string value = Regex.Replace(openingSeries.Groups["series"].Value, @"\s+", " ").Trim();
                        value = value.Trim(' ', '\t', '.', ':', ';', '-', '–', '—', '"', '\'');
                        if (value.Length >= 3 && value.Length <= 100 &&
                            !Regex.IsMatch(value, @"^(?:cover|jacket|interior|copyright|title page|table of contents|publisher|isbn|by)\b", RegexOptions.IgnoreCase))
                        {
                            AddSignal(book, value, "Opening Content", 80);
                        }
                    }
                }
            }

            // Explicit series language in a title is useful local evidence. It
            // does not establish a series number and never outranks EPUB Series.
            AddTitleClues(book, title);

            // Deduplicate same-source repeated observations while retaining
            // different source labels so corroboration can be inspected.
            book.Signals = book.Signals
                .GroupBy(signal => signal.Source + "|" + NormalizeKey(signal.Value), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(signal => signal.Rank).First())
                .ToList();
            return book;
        }

        private static void AddTitleClues(BookSeriesEvidence book, string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return;

            Match novella = NovellaOrStoryPattern.Match(title);
            if (novella.Success)
                AddSignal(book, novella.Groups["series"].Value, "Title subtitle clue", 75);

            Match storiesFrom = StoriesFromPattern.Match(title);
            if (storiesFrom.Success)
                AddSignal(book, storiesFrom.Groups["series"].Value, "Title collection clue", 75);

            Match parenthetical = ParentheticalSeriesPattern.Match(title);
            if (parenthetical.Success)
            {
                string parentheticalValue = parenthetical.Groups["series"].Value.Trim();
                // Parentheses can be edition notes. Accept as series evidence
                // only when a second explicit title clue in this same title
                // agrees, or when a direct Series field independently agrees.
                bool corroborated = book.Signals.Any(signal =>
                    string.Equals(NormalizeKey(signal.Value), NormalizeKey(parentheticalValue), StringComparison.OrdinalIgnoreCase) &&
                    signal.Rank >= 75);
                if (corroborated)
                    AddSignal(book, parentheticalValue, "Corroborating title parenthetical", 65);
            }
        }

        private static bool IsMeaningfulFolderSeriesHint(string folderName, MetadataRecord record)
        {
            if (string.IsNullOrWhiteSpace(folderName))
                return false;

            string normalized = NormalizeKey(folderName);
            if (string.IsNullOrWhiteSpace(normalized) || normalized.All(char.IsDigit))
                return false;

            string[] genericNames =
            {
                "test", "tests", "books", "ebooks", "e books", "library",
                "downloads", "download", "documents", "desktop", "new folder",
                "untitled", "collection", "misc", "miscellaneous", "unsorted",
                "to sort", "to organize", "organized", "output", "input"
            };

            if (genericNames.Any(name => string.Equals(normalized, NormalizeKey(name), StringComparison.OrdinalIgnoreCase)))
                return false;

            string author = record.Metadata?.Author?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(author) ||
                !string.Equals(normalized, NormalizeKey(author), StringComparison.OrdinalIgnoreCase);
        }

        private static int SourceRank(string source)
        {
            if (source.Contains("EPUB", StringComparison.OrdinalIgnoreCase)) return 100;
            if (source.Contains("User", StringComparison.OrdinalIgnoreCase)) return 95;
            if (source.Contains("Front Matter Consultant", StringComparison.OrdinalIgnoreCase)) return 90;
            if (source.Contains("External", StringComparison.OrdinalIgnoreCase)) return 85;
            if (source.Contains("Opening Content", StringComparison.OrdinalIgnoreCase)) return 80;
            if (source.Contains("Title", StringComparison.OrdinalIgnoreCase)) return 75;
            if (source.Contains("Collection", StringComparison.OrdinalIgnoreCase) || source.Contains("Folder", StringComparison.OrdinalIgnoreCase)) return 60;
            if (source.Contains("Filename", StringComparison.OrdinalIgnoreCase)) return 50;
            return 40;
        }

        private static void AddSignal(BookSeriesEvidence book, string? value, string source, int rank)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            string cleaned = value.Trim().Trim(' ', '.', ':', ';', '-', '–', '—');
            if (cleaned.Length < 3 || string.IsNullOrWhiteSpace(NormalizeKey(cleaned))) return;
            book.Signals.Add(new SeriesSignal(cleaned, source, rank));
        }

        private static string NormalizeKey(string value)
        {
            string key = Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
            key = key.Trim(' ', '.', ':', ';', '-', '–', '—');
            key = Regex.Replace(key, @"\s+series$", string.Empty, RegexOptions.IgnoreCase).Trim();
            key = Regex.Replace(key, @"^the\s+", string.Empty, RegexOptions.IgnoreCase).Trim();
            key = Regex.Replace(key, @"[\p{P}\p{S}]+", " ");
            return Regex.Replace(key, @"\s+", " ").Trim().ToLowerInvariant();
        }

        private sealed class BookSeriesEvidence
        {
            public string Path { get; init; } = string.Empty;
            public string Title { get; init; } = string.Empty;
            public string ObservedSeries { get; init; } = string.Empty;
            public List<SeriesSignal> Signals { get; set; } = new();
        }

        private sealed class SeriesSignal
        {
            public SeriesSignal(string value, string source, int rank)
            {
                Value = value;
                Source = source;
                Rank = rank;
            }
            public string Value { get; }
            public string Source { get; }
            public int Rank { get; }
        }
    }
}
