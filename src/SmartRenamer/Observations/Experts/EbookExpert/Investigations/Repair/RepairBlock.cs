using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Models;
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

            foreach (MetadataRecord record in metadataReport.Records)
            {
                E_EbookMetadata metadata = record.Metadata;

                RepairOpportunity opportunity = new()
                {
                    Record = record,

                    MissingTitle =
                        string.IsNullOrWhiteSpace(metadata.Title),

                    MissingAuthor =
                        string.IsNullOrWhiteSpace(metadata.Author),

                    MissingIsbn =
                        string.IsNullOrWhiteSpace(metadata.Isbn),

                    MissingPublisher =
                        string.IsNullOrWhiteSpace(metadata.Publisher),

                    MissingLanguage =
                        string.IsNullOrWhiteSpace(metadata.Language),

                    MissingDescription =
                        string.IsNullOrWhiteSpace(metadata.Description),

                    MissingCover =
                        !metadata.HasCover
                };

                if (opportunity.MissingTitle ||
                    opportunity.MissingAuthor ||
                    opportunity.MissingIsbn ||
                    opportunity.MissingPublisher ||
                    opportunity.MissingLanguage ||
                    opportunity.MissingDescription ||
                    opportunity.MissingCover)
                {
                    report.Opportunities.Add(opportunity);
                }
            }

            report.RepairableBooks =
                report.Opportunities.Count;

            return report;
        }
    }
}