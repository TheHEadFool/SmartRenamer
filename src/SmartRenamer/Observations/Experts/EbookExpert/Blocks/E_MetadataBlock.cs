using SmartRenamer.Models;
using SmartRenamer.Observations.BuildingBlocks;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
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

                AnalyzeMetadata(metadata, report);

                CollectEvidence(
                metadata,
                file);
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
                     _isbnEvidence,
                     metadata.Isbn,
                     file.OriginalName);
            }
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