using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Quality;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations

// Begin namespace
{
    /// <summary>
    /// =========================================================================
    /// E_QualityInvestigation
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Coordinates quality-related investigations performed by the
    /// Ebook Expert.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Coordinate quality Blocks.
    /// • Coordinate quality Consultants.
    /// • Evaluate the overall health of the ebook library.
    /// • Collect observations.
    /// • Report findings back to the Ebook Expert.
    ///
    /// This Investigation does NOT
    /// -------------------------------------------------------------------------
    /// • Read ebook files directly.
    /// • Modify ebook files.
    /// • Communicate with Scout.
    ///
    /// Those responsibilities belong to Consultants and Blocks.
    /// =========================================================================
    /// </summary>
    public sealed class E_QualityInvestigation

    // Begin E_QualityInvestigation
    {
        public List<ExpertFinding> Investigate(
            MetadataReport metadataReport)

        // Begin Investigate()
        {
            List<ExpertFinding> findings = new();

            //---------------------------------------------------------
            // Ask the Block to discover facts.
            //---------------------------------------------------------

            E_QualityBlock block = new();

            QualityReport report =
                block.Analyze(metadataReport);

            //---------------------------------------------------------
            // Ask the Consultant to interpret those facts.
            //---------------------------------------------------------

            E_QualityConsultant consultant = new();

            findings.AddRange(
                consultant.Review(report));

            return findings;

        } // End Investigate()

    } // End E_QualityInvestigation

} // End namespace