using SmartRenamer.Models;
using Scout.Observations.Experts.EbookExpert.Data;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using SmartRenamer.Observations.Experts.EbookExpert.Resources;
using System;
using System.Collections.Generic;

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
            IReadOnlyList<FileContext> files)
        {
            MetadataReport report = new();

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

                report.Records.Add(
                    new MetadataRecord
                    {
                        File = file,
                        Metadata = metadata
                    });

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
                    report);
            }

            CalculateMissingMetadata(report);

            AnalyzeConsistency(report);

            return report;
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
            MetadataReport report)
        {
            AddObservedEvidence(
                report,
                "EPUB Metadata",
                "Title",
                metadata.Title,
                "OPF");

            AddObservedEvidence(
                report,
                "EPUB Metadata",
                "Author",
                metadata.Author,
                "OPF");

            AddObservedEvidence(
                report,
                "EPUB Metadata",
                "Series",
                metadata.Series,
                "OPF");

            AddObservedEvidence(
                report,
                "EPUB Metadata",
                "ISBN",
                metadata.Isbn,
                "OPF");

            string fileName =
                System.IO.Path.GetFileNameWithoutExtension(
                    file.CurrentName);

            AddObservedEvidence(
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
            MetadataReport report,
            E_EpubContentResource.ContentDocument document)
        {
            string value = document.Text.Trim();

            if (string.IsNullOrWhiteSpace(value))
                return;

            if (value.Length > 500)
                value = value[..500];

            report.Evidence.Add(new MetadataEvidence
            {
                Source = "Opening Content",
                Field = "Content",
                Value = value,
                Location = document.Path,
                Notes = "Observed opening-content evidence; not interpreted here."
            });
        }

        private static void AddObservedEvidence(
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