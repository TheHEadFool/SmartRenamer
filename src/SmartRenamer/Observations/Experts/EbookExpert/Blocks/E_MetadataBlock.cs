using SmartRenamer.Models;
using Scout.Observations.Experts.EbookExpert.Data;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using SmartRenamer.Observations.Experts.EbookExpert.Resources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartRenamer.Observations.Experts.EbookExpert.Blocks
{
    /// <summary>
    /// =========================================================================
    /// E_MetadataBlock
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Builds Scout's understanding of ebook metadata.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Read ebook metadata.
    /// • Measure metadata availability.
    /// • Identify missing metadata.
    /// • Detect simple metadata consistency issues.
    /// • Produce a MetadataReport.
    ///
    /// This Block does NOT
    /// -------------------------------------------------------------------------
    /// • Produce ExpertFindings.
    /// • Communicate with Scout.
    /// • Decide what is important.
    ///
    /// Those responsibilities belong to Consultants.
    /// =========================================================================
    /// </summary>
    public class E_MetadataBlock
    {
        //---------------------------------------------------------
        // Consistency Tracking
        //---------------------------------------------------------

        private readonly Dictionary<string, List<string>> _isbnEvidence =
    new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, List<string>> _titleEvidence =
            new(StringComparer.OrdinalIgnoreCase);

        //---------------------------------------------------------

        public MetadataReport Analyze(
            IReadOnlyList<FileContext> files,
            string sourceFolderPath)
        {
            MetadataReport report = new();

            ArgumentException.ThrowIfNullOrWhiteSpace(sourceFolderPath);

            string normalizedSourceFolder =
                System.IO.Path.GetFullPath(sourceFolderPath);

            _isbnEvidence.Clear();
            _titleEvidence.Clear();

            foreach (FileContext file in files)
            {
                report.TotalFiles++;

                if (!file.Extension.Equals(
                        ".epub",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                report.EpubFiles++;

                E_EbookMetadata? metadata =
                    E_EbookMetadataReader.Read(file);

                if (metadata == null)
                {
                    // Future enhancement:
                    // Preserve why the metadata could not be read.
                    continue;
                }

                MetadataRecord record = new()
                {
                    File = file,
                    Metadata = metadata
                };

                report.Records.Add(record);

                AnalyzeMetadata(metadata, report);

                ClassifyMetadata(
                    metadata,
                    report);

                CollectEvidence(
                    metadata,
                    file);

                CollectSourceEvidence(
                    metadata,
                    file,
                    record,
                    report,
                    normalizedSourceFolder);
            }

            //---------------------------------------------------------
            // Collection-context evidence
            //---------------------------------------------------------
            //
            // A folder name is not automatically a series assignment.
            // However, when the selected source folder name is itself
            // observed as a Series value by one or more EPUBs in that
            // collection, that relationship becomes useful collection
            // evidence for the sibling books.
            //
            // This is deliberately recorded as derived collection evidence.
            // It is NOT treated as an independent source and does not
            // change any observed metadata. Domain evaluators may later
            // decide whether this corroborates or conflicts with the
            // evidence for an individual book.
            //---------------------------------------------------------

            PropagateCollectionSeriesEvidence(
                report,
                normalizedSourceFolder);

            CalculateMissingMetadata(report);

            AnalyzeConsistency(report);

            return report;
        }

        private static void PropagateCollectionSeriesEvidence(
            MetadataReport report,
            string sourceFolderPath)
        {
            string sourceFolderName =
                new System.IO.DirectoryInfo(sourceFolderPath).Name.Trim();

            if (string.IsNullOrWhiteSpace(sourceFolderName))
                return;

            List<MetadataRecord> matchingRecords =
                report.Records
                    .Where(record =>
                        string.Equals(
                            record.Metadata.Series?.Trim(),
                            sourceFolderName,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

            if (matchingRecords.Count == 0)
                return;

            List<string> supportingFiles =
                matchingRecords
                    .Select(record => record.File.OriginalFullPath)
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

            foreach (MetadataRecord record in report.Records)
            {
                bool alreadyObserved =
                    string.Equals(
                        record.Metadata.Series?.Trim(),
                        sourceFolderName,
                        StringComparison.OrdinalIgnoreCase);

                if (alreadyObserved)
                    continue;

                bool alreadyRecorded =
                    record.Evidence.Any(evidence =>
                        string.Equals(
                            evidence.Source,
                            "Collection",
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            evidence.Field,
                            "Series",
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            evidence.Value?.Trim(),
                            sourceFolderName,
                            StringComparison.OrdinalIgnoreCase));

                if (alreadyRecorded)
                    continue;

                MetadataEvidence evidence = new()
                {
                    Source = "Collection",
                    Field = "Series",
                    Value = sourceFolderName,
                    Location = sourceFolderPath,
                    Notes =
                        $"Derived collection evidence: {matchingRecords.Count} EPUB metadata record(s) in the same source folder identify the series as '{sourceFolderName}'. This is corroborating context, not an independent source."
                };

                evidence.Files.AddRange(supportingFiles);
                record.Evidence.Add(evidence);
                report.Evidence.Add(evidence);
            }
        }

        //---------------------------------------------------------
        // Metadata Availability
        //---------------------------------------------------------

        private static void AnalyzeMetadata(
            E_EbookMetadata metadata,
            MetadataReport report)
        {
            if (!string.IsNullOrWhiteSpace(metadata.Title))
                report.Titles++;

            if (!string.IsNullOrWhiteSpace(metadata.Author))
                report.Authors++;

            if (!string.IsNullOrWhiteSpace(metadata.Series))
                report.Series++;

            if (!string.IsNullOrWhiteSpace(metadata.Publisher))
                report.Publishers++;

            if (!string.IsNullOrWhiteSpace(metadata.Language))
                report.Languages++;

            if (!string.IsNullOrWhiteSpace(metadata.Isbn))
                report.Isbns++;

            if (!string.IsNullOrWhiteSpace(metadata.Description))
                report.Descriptions++;

            if (metadata.HasCover)
                report.Covers++;

            // Future Reader enhancements:
            // Publication Date
            // Subjects
            // Rights
        }
        //---------------------------------------------------------
        // Metadata Quality
        //---------------------------------------------------------

        private static void ClassifyMetadata(
            E_EbookMetadata metadata,
            MetadataReport report)
        {
            int score = 0;

            if (!string.IsNullOrWhiteSpace(metadata.Title))
                score++;

            if (!string.IsNullOrWhiteSpace(metadata.Author))
                score++;

            if (!string.IsNullOrWhiteSpace(metadata.Publisher))
                score++;

            if (!string.IsNullOrWhiteSpace(metadata.Language))
                score++;

            if (!string.IsNullOrWhiteSpace(metadata.Isbn))
                score++;

            if (!string.IsNullOrWhiteSpace(metadata.Description))
                score++;

            if (!string.IsNullOrWhiteSpace(metadata.Series))
                score++;

            if (metadata.HasCover)
                score++;

            if (score == 8)
            {
                report.ExcellentMetadata++;
                report.CompleteMetadata++;
            }
            else if (score >= 6)
            {
                report.CompleteMetadata++;
            }
            else if (score >= 3)
            {
                report.IncompleteMetadata++;
            }
            else
            {
                report.NeedsAttention++;
            }
        }
        //---------------------------------------------------------
        // Missing Metadata
        //---------------------------------------------------------

        private static void CalculateMissingMetadata(
            MetadataReport report)
        {
            report.MissingTitles =
                report.EpubFiles - report.Titles;

            report.MissingAuthors =
                report.EpubFiles - report.Authors;

            report.MissingSeries =
                report.EpubFiles - report.Series;

            report.MissingPublishers =
                report.EpubFiles - report.Publishers;

            report.MissingLanguages =
                report.EpubFiles - report.Languages;

            report.MissingIsbns =
                report.EpubFiles - report.Isbns;

            report.MissingDescriptions =
                report.EpubFiles - report.Descriptions;

            report.MissingCovers =
                report.EpubFiles - report.Covers;

            report.MissingPublicationDates =
                report.EpubFiles - report.PublicationDates;
        }
        //---------------------------------------------------------
        // Evidence Collection
        //---------------------------------------------------------
        private void CollectEvidence(
    E_EbookMetadata metadata,
    FileContext file)
        {
            if (!string.IsNullOrWhiteSpace(metadata.Isbn))
            {
                AddEvidence(
                    _isbnEvidence,
                    metadata.Isbn,
                    file.CurrentName);
            }

            if (!string.IsNullOrWhiteSpace(metadata.Title))
            {
                AddEvidence(
                    _titleEvidence,
                    metadata.Title,
                    file.OriginalName);
            }
        }

        //---------------------------------------------------------
        // Source Evidence
        //---------------------------------------------------------

        private static void CollectSourceEvidence(
            E_EbookMetadata metadata,
            FileContext file,
            MetadataRecord record,
            MetadataReport report,
            string sourceFolderPath)
        {
            AddObservedEvidence(
                record,
                report,
                "EPUB Metadata",
                "Title",
                metadata.Title,
                "OPF");

            AddObservedEvidence(
                record,
                report,
                "EPUB Metadata",
                "Author",
                metadata.Author,
                "OPF");

            AddObservedEvidence(
                record,
                report,
                "EPUB Metadata",
                "Series",
                metadata.Series,
                "OPF");

            AddObservedEvidence(
                record,
                report,
                "EPUB Metadata",
                "SeriesNumber",
                metadata.SeriesNumber,
                "OPF");

            AddObservedEvidence(
                record,
                report,
                "EPUB Metadata",
                "ISBN",
                metadata.Isbn,
                "OPF");

            //---------------------------------------------------------
            // Collection-context evidence
            //---------------------------------------------------------
            //
            // The selected source folder is an observation about the
            // collection, not a conclusion about the individual book.
            // Preserve the folder name as evidence so downstream domain
            // evaluators can decide whether it supports or contradicts
            // a candidate. Do not interpret it here.
            //
            //---------------------------------------------------------

            string sourceFolderName =
                new System.IO.DirectoryInfo(sourceFolderPath).Name;

            AddObservedEvidence(
                record,
                report,
                "Source Folder",
                "Series",
                sourceFolderName,
                sourceFolderPath,
                "Collection-context evidence; folder name may support or contradict a series candidate and is not interpreted here.");

            string fileName =
                System.IO.Path.GetFileNameWithoutExtension(
                    file.CurrentName);

            AddObservedEvidence(
                record,
                report,
                "Filename",
                "Filename",
                fileName,
                file.CurrentName,
                "Candidate identity evidence; not interpreted here.");

            try
            {
                E_EpubContentResource resource = new();

                IReadOnlyList<E_EpubContentResource.ContentDocument> documents =
                    resource.ExtractOpeningDocuments(
                        file.CurrentFullPath,
                        10);

                foreach (E_EpubContentResource.ContentDocument document in documents)
                {
                    AddOpeningContentEvidence(
                        record,
                        report,
                        document);
                }
            }
            catch
            {
                // Metadata reading remains tolerant of malformed EPUBs.
                // Content evidence is supplemental and must not make the
                // metadata investigation fail.
            }
        }

        private static void AddOpeningContentEvidence(
            MetadataRecord record,
            MetadataReport report,
            E_EpubContentResource.ContentDocument document)
        {
            string value = document.Text.Trim();

            if (string.IsNullOrWhiteSpace(value))
                return;

            if (value.Length > 500)
                value = value[..500];

            MetadataEvidence evidence = new()
            {
                Source = "Opening Content",
                Field = "Content",
                Value = value,
                Location = document.Path,
                Notes = "Observed opening-content evidence; not interpreted here."
            };

            record.Evidence.Add(evidence);
            report.Evidence.Add(evidence);
        }

        private static void AddObservedEvidence(
            MetadataRecord record,
            MetadataReport report,
            string source,
            string field,
            string value,
            string location,
            string notes = "Observed metadata; not independently corroborated.")
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            MetadataEvidence evidence = new()
            {
                Source = source,
                Field = field,
                Value = value.Trim(),
                Location = location,
                Notes = notes
            };

            evidence.Files.Add(location);
            record.Evidence.Add(evidence);
            report.Evidence.Add(evidence);
        }

        //---------------------------------------------------------
        // Consistency
        //---------------------------------------------------------


        //---------------------------------------------------------

        private void AnalyzeConsistency(
            MetadataReport report)
        {
            foreach (KeyValuePair<string, List<string>> pair in _isbnEvidence)
            {
                if (pair.Value.Count > 1)
                {
                    report.DuplicateIsbns++;

                    MetadataEvidence evidence = new()
                    {
                        Category = "Duplicate ISBN",
                        Value = pair.Key
                    };

                    evidence.Files.AddRange(pair.Value);

                    report.Evidence.Add(evidence);
                }
            }

            foreach (KeyValuePair<string, List<string>> pair in _titleEvidence)
            {
                if (pair.Value.Count > 1)
                {
                    report.DuplicateTitles++;

                    MetadataEvidence evidence = new()
                    {
                        Category = "Duplicate Title",
                        Value = pair.Key
                    };

                    evidence.Files.AddRange(pair.Value);

                    report.Evidence.Add(evidence);
                }
            }

            // Future:
            // Conflicting Authors
            // Conflicting Series
        }

        //---------------------------------------------------------

        private static void AddEvidence(
            IDictionary<string, List<string>> dictionary,
    string key,
    string fileName)
        {
            if (!dictionary.TryGetValue(
                    key,
                    out List<string>? files))
            {
                files = new List<string>();

                dictionary[key] = files;
            }

            files.Add(fileName);
        }

    }
}