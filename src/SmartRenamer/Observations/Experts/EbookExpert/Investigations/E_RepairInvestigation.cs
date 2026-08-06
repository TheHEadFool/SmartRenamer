using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations

// Begin namespace
{
    /// <summary>
    /// =========================================================================
    /// E_RepairInvestigation
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Coordinates repair-related investigations performed by the
    /// Ebook Expert.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Coordinate Repair Blocks.
    /// • Coordinate Repair Consultants.
    /// • Collect repair opportunities.
    /// • Report findings back to the Ebook Expert.
    ///
    /// This Investigation does NOT
    /// -------------------------------------------------------------------------
    /// • Modify ebook files.
    /// • Repair metadata.
    /// • Communicate with Scout.
    ///
    /// Those responsibilities belong to Blocks and Consultants.
    /// =========================================================================
    /// </summary>
    public sealed class E_RepairInvestigation

    // Begin E_RepairInvestigation
    {
        public List<ExpertFinding> Investigate(
            MetadataReport metadataReport)

        // Begin Investigate()
        {
            List<ExpertFinding> findings = new();

            //---------------------------------------------------------
            // Ask the Block to discover facts.
            //---------------------------------------------------------

            RepairBlock block = new();

            RepairReport report =
                block.Analyze(metadataReport);

            //---------------------------------------------------------
            // Ask the Consultant to interpret those facts.
            //---------------------------------------------------------

            E_RepairConsultant consultant = new();

            findings.AddRange(
                consultant.Review(report));

            return findings;

        } // End Investigate()

    } // End E_RepairInvestigation

} // End namespace