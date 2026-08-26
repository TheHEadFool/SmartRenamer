using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;

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

                //---------------------------------------------------------
                // Preserve aggregate repair facts and evidence.
                //---------------------------------------------------------

                if (opportunity.MissingTitle)
                {
                    report.MissingTitles++;
                    report.MissingTitleBooks.Add(
                        record.File.CurrentName);
                }

                if (opportunity.MissingAuthor)
                {
                    report.MissingAuthors++;
                    report.MissingAuthorBooks.Add(
                        record.File.CurrentName);
                }

                if (opportunity.MissingIsbn)
                {
                    report.MissingIsbns++;
                    report.MissingIsbnBooks.Add(
                        record.File.CurrentName);
                }

                if (opportunity.MissingPublisher)
                {
                    report.MissingPublishers++;
                    report.MissingPublisherBooks.Add(
                        record.File.CurrentName);
                }

                if (opportunity.MissingLanguage)
                {
                    report.MissingLanguages++;
                    report.MissingLanguageBooks.Add(
                        record.File.CurrentName);
                }

                if (opportunity.MissingDescription)
                {
                    report.MissingDescriptions++;
                    report.MissingDescriptionBooks.Add(
                        record.File.CurrentName);
                }

                if (opportunity.MissingCover)
                {
                    report.MissingCovers++;
                    report.MissingCoverBooks.Add(
                        record.File.CurrentName);
                }

                //---------------------------------------------------------
                // Preserve the complete structured opportunity.
                //
                // A book is included only when at least one repair
                // opportunity exists.
                //---------------------------------------------------------

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

            //---------------------------------------------------------
            // A book is repairable for investigation purposes when
            // at least one known repair opportunity exists.
            //
            // This does NOT mean Scout should automatically repair it.
            //---------------------------------------------------------

            report.RepairableBooks =
                report.Opportunities.Count;

            return report;
        }
    }
}