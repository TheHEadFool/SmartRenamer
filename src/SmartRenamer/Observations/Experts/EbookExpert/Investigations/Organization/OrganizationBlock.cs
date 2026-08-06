using SmartRenamer.Models;
using SmartRenamer.Observations.BuildingBlocks;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using System;
using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization
{
    /// <summary>
    /// =========================================================================
    /// OrganizationBlock
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Researches how an ebook collection is organized.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Observe organization.
    /// • Collect organization evidence.
    /// • Produce an OrganizationReport.
    ///
    /// This Block does NOT
    /// -------------------------------------------------------------------------
    /// • Interpret significance.
    /// • Produce recommendations.
    /// • Communicate with Scout.
    ///
    /// Those responsibilities belong to the Consultant.
    /// =========================================================================
    /// </summary>
    public class OrganizationBlock
    {
        //---------------------------------------------------------
        // Series Tracking
        //---------------------------------------------------------

        private readonly Dictionary<string, List<string>> _series =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, List<string>> _publishers =
    new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, List<string>> _languages =
    new(StringComparer.OrdinalIgnoreCase);

        //---------------------------------------------------------

        public OrganizationReport Analyze(
    MetadataReport metadataReport)
        {
            OrganizationReport report = new();

            _series.Clear();
            _publishers.Clear();
            _languages.Clear();

            foreach (MetadataRecord record in metadataReport.Records)
            {
                FileContext file = record.File;
                E_EbookMetadata metadata = record.Metadata;

                if (string.IsNullOrWhiteSpace(metadata.Series))
                {
                    report.BooksWithoutSeries++;
                }

                CollectSeries(
                    metadata,
                    file);

                CollectPublisher(
                    metadata,
                    file);

                CollectLanguage(
                    metadata,
                    file);

            }

            BuildSeriesReport(report);

            return report;
        }

        //---------------------------------------------------------
        // Series
        //---------------------------------------------------------

        private void CollectSeries(
            E_EbookMetadata metadata,
            FileContext file)
        {
            if (string.IsNullOrWhiteSpace(metadata.Series))
            {
                return;
            }

            if (!_series.TryGetValue(
                    metadata.Series,
                    out List<string>? books))
            {
                books = new List<string>();

                _series.Add(
                    metadata.Series,
                    books);
            }

            books.Add(file.CurrentName);
        }

        private void CollectPublisher(
    E_EbookMetadata metadata,
    FileContext file)
        {
            if (string.IsNullOrWhiteSpace(metadata.Publisher))
            {
                return;
            }

            if (!_publishers.TryGetValue(
                    metadata.Publisher,
                    out List<string>? books))
            {
                books = new List<string>();

                _publishers.Add(
                    metadata.Publisher,
                    books);
            }

            books.Add(file.CurrentName);
        }
        //---------------------------------------------------------

        private void CollectLanguage(
    E_EbookMetadata metadata,
    FileContext file)
        {
            if (string.IsNullOrWhiteSpace(metadata.Language))
            {
                return;
            }

            if (!_languages.TryGetValue(
                    metadata.Language,
                    out List<string>? books))
            {
                books = new List<string>();

                _languages.Add(
                    metadata.Language,
                    books);
            }

            books.Add(file.CurrentName);
        }

        private void BuildSeriesReport(
            OrganizationReport report)

            
        {

            report.SeriesCount = _series.Count;

            report.PublisherCount = _publishers.Count;

            report.LanguageCount = _languages.Count;

            foreach (KeyValuePair<string, List<string>> pair in _series)
            {
                int count = pair.Value.Count;

                report.BooksInSeries += count;

                if (count == 1)
                {
                    report.SingleBookSeries++;
                }

                if (count > report.LargestSeriesSize)
                {
                    report.LargestSeriesSize = count;
                }


                OrganizationEvidence evidence = new()
                {
                    Category = "Series",
                    Value = pair.Key
                };

                evidence.Files.AddRange(pair.Value);

                report.Evidence.Add(evidence);


            }
        }
    }
}