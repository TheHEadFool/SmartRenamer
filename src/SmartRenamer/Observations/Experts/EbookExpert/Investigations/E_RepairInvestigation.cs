using System;
using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations
{
    /// <summary>
    /// =========================================================================
    /// E_RepairInvestigation
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Coordinates repair-related investigations performed by the Ebook Expert.
    ///
    /// The Investigation discovers repair opportunities and preserves them so
    /// that later domain operations can act on the specific ebooks involved.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Coordinate the Repair Block.
    /// • Coordinate the Repair Consultant.
    /// • Collect repair opportunities.
    /// • Produce ExpertFindings.
    /// • Preserve the discovered RepairOpportunities for later repair work.
    ///
    /// This Investigation does NOT
    /// -------------------------------------------------------------------------
    /// • Perform external research.
    /// • Modify ebook files.
    /// • Automatically approve repairs.
    /// • Communicate with Scout.
    ///
    /// Those responsibilities belong to the Repair Resources,
    /// Repair Service, Conversation Framework, and User.
    /// =========================================================================
    /// </summary>
    public sealed class E_RepairInvestigation
    {
        private RepairReport? _lastReport;

        /// <summary>
        /// Investigates repair opportunities using the shared metadata report.
        /// </summary>
        public List<ExpertFinding> Investigate(
            MetadataReport metadataReport)
        {
            if (metadataReport == null)
                throw new ArgumentNullException(nameof(metadataReport));

            List<ExpertFinding> findings = new();

            //---------------------------------------------------------
            // Ask the Block to discover factual repair opportunities.
            //---------------------------------------------------------

            RepairBlock block = new();

            RepairReport report =
                block.Analyze(metadataReport);

            //---------------------------------------------------------
            // Preserve the complete report.
            //
            // The report contains the specific RepairOpportunity objects
            // associated with the ebooks that require attention.
            //---------------------------------------------------------

            _lastReport = report;

            //---------------------------------------------------------
            // Ask the Consultant to interpret the discovered facts.
            //---------------------------------------------------------

            E_RepairConsultant consultant = new();

            findings.AddRange(
                consultant.Review(report));

            return findings;
        }

        /// <summary>
        /// The repair opportunities discovered during the most recent
        /// investigation.
        ///
        /// These are the actual domain records that can later be supplied
        /// to E_RepairService.
        /// </summary>
        public IReadOnlyList<RepairOpportunity> RepairOpportunities =>
            _lastReport?.Opportunities
            ?? new List<RepairOpportunity>();
    }
}