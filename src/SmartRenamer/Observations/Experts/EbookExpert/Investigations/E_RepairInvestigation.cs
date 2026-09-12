using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly E_RepairExpedition _repairExpedition = new();
        private readonly E_RepairAuthorization _repairAuthorization = new();

        public E_RepairAuthorization RepairAuthorization =>
            _repairAuthorization;

        public void AuthorizeAutomaticRepairs()
        {
            _repairAuthorization.AuthorizeAutomaticRepairs();
        }


        public void BeginExpedition(
    string sourceFolderPath,
    IReadOnlyList<FileContext> files)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceFolderPath);
            ArgumentNullException.ThrowIfNull(files);

            if (string.Equals(
                    _repairExpedition.SourceFolderPath,
                    sourceFolderPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _repairAuthorization.Clear();

            _repairExpedition.Begin(
                sourceFolderPath,
                files);
        }

        /// <summary>
        /// Investigates repair opportunities using the shared metadata report.
        /// </summary>
        public List<ExpertFinding> Investigate(
            MetadataReport metadataReport)
        {
            ArgumentNullException.ThrowIfNull(metadataReport);

            List<ExpertFinding> findings = new();

            //---------------------------------------------------------
            // The Repair Expedition works one EPUB at a time.
            //
            // Initial investigation must not stop on an EPUB that is
            // already complete. If the first EPUB needs no repair, move
            // forward until the expedition reaches the first EPUB that
            // actually requires attention.
            //
            // This preserves the one-ebook-at-a-time repair lifecycle
            // while allowing a collection-wide investigation to enter
            // the ISBN vertical slice at the first real repair need.
            //---------------------------------------------------------

            RepairBlock block = new();
            E_RepairConsultant consultant = new();

            while (_repairExpedition.CurrentFile != null)
            {
                FileContext currentFile =
                    _repairExpedition.CurrentFile;

                SmartRenamer.Observations.Experts.EbookExpert.Data.Models.MetadataRecord? currentRecord =
                    metadataReport.Records.FirstOrDefault(
                        record =>
                            string.Equals(
                                record.File?.OriginalFullPath,
                                currentFile.OriginalFullPath,
                                StringComparison.OrdinalIgnoreCase));

                //-----------------------------------------------------
                // If the current EPUB cannot be matched to the shared
                // metadata report, do not silently advance past it.
                // The metadata investigation has not established that
                // this EPUB is complete.
                //-----------------------------------------------------

                if (currentRecord == null)
                {
                    _lastReport = new RepairReport();
                    return findings;
                }

                MetadataReport currentMetadataReport =
                    new();

                currentMetadataReport.Records.Add(
                    currentRecord);

                RepairReport report =
                    block.Analyze(currentMetadataReport);

                //-----------------------------------------------------
                // Preserve the report for the current EPUB.
                //-----------------------------------------------------

                _lastReport = report;

                //-----------------------------------------------------
                // A complete EPUB requires no repair conversation.
                // Mark it complete and advance the expedition.
                //-----------------------------------------------------

                if (report.IsComplete)
                {
                    _repairExpedition.CompleteCurrent();
                    continue;
                }

                //-----------------------------------------------------
                // The expedition has reached an EPUB that requires
                // attention. Let the Consultant describe that finding.
                //-----------------------------------------------------

                findings.AddRange(
                    consultant.Review(report));

                return findings;
            }

            //---------------------------------------------------------
            // No EPUBs remain that require repair.
            //---------------------------------------------------------

            _lastReport = new RepairReport();

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

        /// <summary>
        /// True when the most recent repair investigation found no remaining
        /// repair opportunities.
        /// </summary>
        public bool IsComplete =>
            _lastReport?.IsComplete ?? false;

        public FileContext? CurrentFile =>
    _repairExpedition.CurrentFile;

        public IReadOnlyList<FileContext> DeferredFiles =>
            _repairExpedition.DeferredFiles;

        public bool HasDeferredFiles =>
            _repairExpedition.HasDeferredFiles;

        public bool ExpeditionIsComplete =>
            _repairExpedition.IsComplete;

        public bool CompleteCurrentIfComplete()
        {
            if (_repairExpedition.CurrentFile == null)
                return false;

            if (!IsCompleteFor(
                    _repairExpedition.CurrentFile.OriginalFullPath))
                return false;

            _repairExpedition.CompleteCurrent();

            return true;
        }

        public bool DeferCurrent()
        {
            if (_repairExpedition.CurrentFile == null)
                return false;

            _repairExpedition.DeferCurrent();

            return true;
        }

        /// <summary>
        /// Determines whether the specified EPUB has any remaining repair
        /// opportunities in the most recent investigation.
        ///
        /// The original full path is the stable identity of the EPUB.
        /// </summary>
        public bool IsCompleteFor(string originalFullPath)
        {
            if (string.IsNullOrWhiteSpace(originalFullPath))
                throw new ArgumentException(
                    "Original EPUB path cannot be empty.",
                    nameof(originalFullPath));

            RepairOpportunity? opportunity =
                _lastReport?.Opportunities.FirstOrDefault(
                    item => string.Equals(
                        item.Record?.File?.OriginalFullPath,
                        originalFullPath,
                        StringComparison.OrdinalIgnoreCase));

            // If the EPUB no longer appears in the repair opportunities,
            // the current investigation considers it complete.
            return opportunity == null || opportunity.IsComplete;
        }

    }



}