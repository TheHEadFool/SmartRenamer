using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Enrichment;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations

// Begin namespace
{
    /// <summary>
    /// =========================================================================
    /// E_EnrichmentInvestigation
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Coordinates enrichment-related investigations performed by the
    /// Ebook Expert.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Coordinate Enrichment Blocks.
    /// • Coordinate Enrichment Consultants.
    /// • Collect enrichment opportunities.
    /// • Report findings back to the Ebook Expert.
    ///
    /// This Investigation does NOT
    /// -------------------------------------------------------------------------
    /// • Download metadata.
    /// • Modify ebook files.
    /// • Communicate with Scout.
    ///
    /// Those responsibilities belong to Blocks and Consultants.
    /// =========================================================================
    /// </summary>
    public sealed class E_EnrichmentInvestigation

    // Begin E_EnrichmentInvestigation
    {
        public List<ExpertFinding> Investigate(
            MetadataReport metadataReport)

        // Begin Investigate()
        {
            List<ExpertFinding> findings = new();

            //---------------------------------------------------------
            // Ask the Block to discover facts.
            //---------------------------------------------------------

            EnrichmentBlock block = new();

            EnrichmentReport report =
                block.Analyze(metadataReport);

            //---------------------------------------------------------
            // Ask the Consultant to interpret those facts.
            //---------------------------------------------------------

            E_EnrichmentConsultant consultant = new();

            findings.AddRange(
                consultant.Review(report));

            return findings;

        } // End Investigate()

    } // End E_EnrichmentInvestigation

} // End namespace