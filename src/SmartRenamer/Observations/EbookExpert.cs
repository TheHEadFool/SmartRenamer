using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations;
using SmartRenamer.Observations.Specialists;
using System.Collections.Generic;
using Scout.Observations.Conversation;
using SmartRenamer.Observations.Experts.EbookExpert.Translators;

namespace SmartRenamer.Observations

// Begin namespace
{
    /// <summary>
    /// =========================================================================
    /// EbookExpert
    /// =========================================================================
    ///
    /// Scout asks this expert whenever it wants to understand whether a folder
    /// contains a meaningful collection of ebooks.
    ///
    /// The Ebook Expert coordinates a series of Investigations. Each
    /// Investigation asks one or more Blocks to discover facts, then asks
    /// Consultants to interpret those facts into ExpertFindings.
    /// =========================================================================
    /// </summary>
    public sealed class EbookExpert

    // Begin EbookExpert
        : ObservationExpert
    {
        //---------------------------------------------------------
        // Investigations
        //---------------------------------------------------------

        private readonly E_MetadataInvestigation _metadataInvestigation = new();

        private readonly E_ContentsInvestigation _contentsInvestigation = new();

        private readonly E_OrganizationInvestigation _organizationInvestigation = new();

        private readonly E_DuplicateInvestigation _duplicateInvestigation = new();

        private readonly E_QualityInvestigation _qualityInvestigation = new();

        private readonly E_RepairInvestigation _repairInvestigation = new();

        private readonly E_EnrichmentInvestigation _enrichmentInvestigation = new();

        //---------------------------------------------------------
        // Legacy Specialists
        //---------------------------------------------------------

        private static readonly IReadOnlyList<ObservationSpecialist> _specialists =
        [
            new E_EbookMetadataSpecialist()
        ];

        public override IReadOnlyList<ObservationSpecialist> Specialists =>
            _specialists;

        //---------------------------------------------------------

        public override string Name =>
            "eBook Library";

        public override string Summary =>
            "I noticed what appears to be a collection of ebooks.";

        public override string WhyItMatters =>
            "Keeping ebooks organized by author, series, or subject makes your library easier to browse and enjoy.";

        /// <summary>
        /// =========================================================================
        /// Generation 2 Entry Point
        /// =========================================================================
        /// </summary>
        public override List<ExpertFinding> Investigate(
            IReadOnlyList<FileContext> files)

        // Begin Investigate()
        {
            List<ExpertFinding> findings = new();

            //---------------------------------------------------------
            // Acquire metadata once.
            //---------------------------------------------------------

            MetadataReport metadataReport =
                _metadataInvestigation.Investigate(files);

            //---------------------------------------------------------
            // Completed Generation 2 Investigations
            //---------------------------------------------------------

            findings.AddRange(
                _contentsInvestigation.Investigate(
                    metadataReport));

            findings.AddRange(
                _organizationInvestigation.Investigate(
                    metadataReport));

            findings.AddRange(
                _duplicateInvestigation.Investigate(
                    files));

            findings.AddRange(
                _qualityInvestigation.Investigate(
                    metadataReport));

            findings.AddRange(
                _repairInvestigation.Investigate(
                    metadataReport));

            //---------------------------------------------------------
            // Future Investigations
            //---------------------------------------------------------

            findings.AddRange(
                 _enrichmentInvestigation.Investigate(
                     metadataReport));

            return findings;

        } // End Investigate()

        public override List<CV_Recommendation> BuildRecommendations(
    IReadOnlyList<ExpertFinding> findings)
        {
            E_RecommendationTranslator translator = new();

            List<CV_Recommendation> recommendations = new();

            foreach (ExpertFinding finding in findings)
            {
                recommendations.Add(
                    translator.Translate(finding));
            }

            return recommendations;
        }

    } // End EbookExpert

} // End namespace