using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations

// Begin namespace
{
    /// <summary>
    /// =========================================================================
    /// E_OrganizationInvestigation
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Coordinates organization-related investigations performed by the
    /// Ebook Expert.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Coordinate organization Blocks.
    /// • Coordinate organization Consultants.
    /// • Evaluate series organization.
    /// • Evaluate author organization.
    /// • Collect observations.
    /// • Report findings back to the Ebook Expert.
    ///
    /// This Investigation does NOT
    /// -------------------------------------------------------------------------
    /// • Read ebook files directly.
    /// • Move or rename files.
    /// • Communicate with Scout.
    ///
    /// Those responsibilities belong to Consultants and Blocks.
    /// =========================================================================
    /// </summary>
    public sealed class E_OrganizationInvestigation

    // Begin E_OrganizationInvestigation
    {
        public List<ExpertFinding> Investigate(
            MetadataReport metadataReport)

        // Begin Investigate()
        {
            List<ExpertFinding> findings = new();

            //---------------------------------------------------------
            // Ask the Block to discover facts.
            //---------------------------------------------------------

            OrganizationBlock block = new();

            OrganizationReport report =
                block.Analyze(metadataReport);

            //---------------------------------------------------------
            // Ask the Consultant to interpret those facts.
            //---------------------------------------------------------

            E_OrganizationConsultant consultant = new();

            findings.AddRange(
                consultant.Review(report));

            return findings;

        } // End Investigate()

    } // End E_OrganizationInvestigation

} // End namespace