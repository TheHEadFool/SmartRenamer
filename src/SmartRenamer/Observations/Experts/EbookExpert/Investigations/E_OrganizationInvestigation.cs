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
    ///
    /// Report Lifetime
    /// -------------------------------------------------------------------------
    /// The OrganizationReport is retained in memory while the current
    /// expedition is active so that later stages can use the organization
    /// facts discovered during investigation.
    ///
    /// The report is working expedition data. It is not permanent library
    /// state and should be released when the expedition is complete.
    /// =========================================================================
    /// </summary>
    public sealed class E_OrganizationInvestigation

    // Begin E_OrganizationInvestigation
    {
        /// <summary>
        /// The organization report produced by the current investigation.
        /// This remains available for the lifetime of the current expedition.
        /// </summary>
        public OrganizationReport? Report { get; private set; }

        public List<ExpertFinding> Investigate(
            MetadataReport metadataReport)

        // Begin Investigate()
        {
            List<ExpertFinding> findings = new();

            //---------------------------------------------------------
            // Ask the Block to discover facts.
            //---------------------------------------------------------

            OrganizationBlock block = new();

            Report =
                block.Analyze(metadataReport);

            //---------------------------------------------------------
            // Ask the Consultant to interpret those facts.
            //---------------------------------------------------------

            E_OrganizationConsultant consultant = new();

            findings.AddRange(
                consultant.Review(Report));

            return findings;

        } // End Investigate()

    } // End E_OrganizationInvestigation

} // End namespace