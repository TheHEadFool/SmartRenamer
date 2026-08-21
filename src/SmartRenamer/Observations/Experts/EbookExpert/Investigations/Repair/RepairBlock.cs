using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using System.Linq;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// =========================================================================
    /// RepairBlock
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Collects factual information describing repair opportunities
    /// within an ebook collection.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Detect missing metadata.
    /// • Detect incomplete metadata.
    /// • Detect repair opportunities.
    /// • Produce a RepairReport.
    ///
    /// This Block does NOT
    /// -------------------------------------------------------------------------
    /// • Repair ebook files.
    /// • Make recommendations.
    /// • Communicate with Scout.
    ///
    /// Those responsibilities belong to the Consultant.
    /// =========================================================================
    /// </summary>
    internal sealed class RepairBlock
    {
        public RepairReport Analyze(
            MetadataReport metadataReport)
        {
            RepairReport report = new();

            //---------------------------------------------------------
            // Examine the metadata records already produced by the
            // Metadata Block.
            //
            // RepairBlock does not read EPUB files itself.
            // It works from the factual metadata supplied to it.
            //---------------------------------------------------------

            foreach (var record in metadataReport.Records)
            {
                if (string.IsNullOrWhiteSpace(record.Metadata.Title))
                    report.MissingTitles++;

                if (string.IsNullOrWhiteSpace(record.Metadata.Author))
                    report.MissingAuthors++;

                if (string.IsNullOrWhiteSpace(record.Metadata.Isbn))
                    report.MissingIsbns++;

                if (string.IsNullOrWhiteSpace(record.Metadata.Publisher))
                    report.MissingPublishers++;

                if (string.IsNullOrWhiteSpace(record.Metadata.Language))
                    report.MissingLanguages++;

                if (string.IsNullOrWhiteSpace(record.Metadata.Description))
                    report.MissingDescriptions++;

                if (!record.Metadata.HasCover)
                    report.MissingCovers++;
            }

            //---------------------------------------------------------
            // A repairable book is one with at least one known
            // metadata repair opportunity.
            //---------------------------------------------------------

            report.RepairableBooks =
                metadataReport.Records.Count(record =>
                    string.IsNullOrWhiteSpace(record.Metadata.Title) ||
                    string.IsNullOrWhiteSpace(record.Metadata.Author) ||
                    string.IsNullOrWhiteSpace(record.Metadata.Isbn) ||
                    string.IsNullOrWhiteSpace(record.Metadata.Publisher) ||
                    string.IsNullOrWhiteSpace(record.Metadata.Language) ||
                    string.IsNullOrWhiteSpace(record.Metadata.Description) ||
                    !record.Metadata.HasCover);

            return report;
        }
    }
}