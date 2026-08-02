using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations;
using SmartRenamer.Observations.Specialists;

namespace SmartRenamer.Observations
{
    /// <summary>
    /// Scout asks this expert whenever it wants to understand whether a folder
    /// contains a meaningful collection of ebooks.
    /// </summary>
    public class EbookExpert : ObservationExpert
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
        // Legacy Specialists (temporary during migration)
        //---------------------------------------------------------

        private static readonly IReadOnlyList<ObservationSpecialist> _specialists =
        [
            new E_EbookMetadataSpecialist()
        ];

        public override IReadOnlyList<ObservationSpecialist> Specialists =>
            _specialists;

        //---------------------------------------------------------

        public override string Name => "eBook Library";

        public override string Summary =>
            "I noticed what appears to be a collection of ebooks.";

        public override string WhyItMatters =>
            "Keeping ebooks organized by author, series, or subject makes your library easier to browse and enjoy.";

        /// <summary>
        /// Generation 2 entry point.
        /// The Ebook Expert coordinates its Investigations and returns
        /// the combined findings.
        /// </summary>
        public override List<ExpertFinding> Investigate(
            IReadOnlyList<FileContext> files)
        {
            List<ExpertFinding> findings = new();

            findings.AddRange(
                _metadataInvestigation.Investigate(files));

            // Future investigations
            //
            // findings.AddRange(_contentsInvestigation.Investigate(files));
            // findings.AddRange(_organizationInvestigation.Investigate(files));
            // findings.AddRange(_duplicateInvestigation.Investigate(files));
            // findings.AddRange(_qualityInvestigation.Investigate(files));
            // findings.AddRange(_repairInvestigation.Investigate(files));
            // findings.AddRange(_enrichmentInvestigation.Investigate(files));

            return findings;
        }
    }
}