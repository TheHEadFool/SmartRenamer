using System;
using System.Collections.Generic;
using System.Linq;
using Scout.Observations.Experts.EbookExpert.Data;
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
    /// • Preserve the collection-level repair opportunities.
    /// • Preserve state for the current repair expedition.
    /// • Produce ExpertFindings.
    /// • Produce semantic repair handoffs.
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

        //---------------------------------------------------------
        // Collection-level repair report
        //
        // This contains the repair opportunities for the entire
        // metadata collection. Collection-level actions such as
        // "Research Missing ISBNs" use this report.
        //---------------------------------------------------------

        private RepairReport? _collectionReport;

        //---------------------------------------------------------
        // Semantic repair handoffs
        //---------------------------------------------------------

        private readonly List<E_RepairHandoff> _repairHandoffs = new();

        //---------------------------------------------------------
        // Repair expedition
        //---------------------------------------------------------

        private readonly E_RepairExpedition _repairExpedition = new();

        //---------------------------------------------------------
        // Repair authorization
        //---------------------------------------------------------

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

            _repairHandoffs.Clear();

            _collectionReport = null;
            _lastReport = null;

            _repairExpedition.Begin(
                sourceFolderPath,
                files);
        }

        /// <summary>
        /// Investigates repair opportunities using the shared metadata report.
        ///
        /// The complete collection is analyzed first so that collection-level
        /// actions retain access to every applicable repair opportunity.
        ///
        /// The Repair Expedition then works one EPUB at a time and stores the
        /// current EPUB's report separately in _lastReport.
        /// </summary>
        public List<ExpertFinding> Investigate(
            MetadataReport metadataReport)
        {
            ArgumentNullException.ThrowIfNull(metadataReport);

            List<ExpertFinding> findings = new();

            //---------------------------------------------------------
            // Preserve the collection-wide repair opportunities.
            //
            // This report is intentionally separate from _lastReport.
            // Collection-level actions such as ISBN research need to
            // see all applicable EPUBs, not only the current expedition
            // EPUB.
            //---------------------------------------------------------

            RepairBlock block = new();

            _collectionReport =
                block.Analyze(metadataReport);

            //---------------------------------------------------------
            // The Repair Expedition works one EPUB at a time.
            //
            // Initial investigation must not stop on an EPUB that is
            // already complete. If the first EPUB needs no repair, move
            // forward until the expedition reaches the first EPUB that
            // actually requires attention.
            //---------------------------------------------------------

            E_RepairConsultant consultant = new();

            while (_repairExpedition.CurrentFile != null)
            {
                FileContext currentFile =
                    _repairExpedition.CurrentFile;

                MetadataRecord? currentRecord =
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
                //
                // This remains separate from _collectionReport.
                //-----------------------------------------------------

                _lastReport = report;

                //-----------------------------------------------------
                // A complete EPUB requires no repair conversation.
                // Mark it complete and advance the expedition.
                //-----------------------------------------------------

                if (report.IsComplete)
                {
                    CreateCompletedHandoff(currentFile);

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
        /// The repair opportunities available to domain actions.
        ///
        /// Collection-level actions use the complete collection report.
        /// The current EPUB remains available through CurrentFile and
        /// IsCompleteFor().
        /// </summary>
        internal IReadOnlyList<RepairOpportunity> RepairOpportunities =>
            _collectionReport?.Opportunities
            ?? new List<RepairOpportunity>();

        /// <summary>
        /// The repair opportunities discovered for the current EPUB.
        ///
        /// This is intentionally separate from RepairOpportunities because
        /// the collection-level action layer may need all EPUBs while the
        /// expedition itself continues to operate one EPUB at a time.
        /// </summary>
        internal IReadOnlyList<RepairOpportunity> CurrentRepairOpportunities =>
            _lastReport?.Opportunities
            ?? new List<RepairOpportunity>();

        /// <summary>
        /// Semantic results produced by the repair stage.
        /// </summary>
        internal IReadOnlyList<E_RepairHandoff> RepairHandoffs =>
            _repairHandoffs;

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

            FileContext currentFile =
                _repairExpedition.CurrentFile;

            if (!IsCompleteFor(
                    currentFile.OriginalFullPath))
                return false;

            CreateCompletedHandoff(currentFile);

            _repairExpedition.CompleteCurrent();

            return true;
        }

        public bool DeferCurrent()
        {
            if (_repairExpedition.CurrentFile == null)
                return false;

            FileContext currentFile =
                _repairExpedition.CurrentFile;

            CreateHandoff(
                currentFile,
                E_RepairHandoffStatus.RepairDeferred,
                "Repair was deferred by the user.");

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

        /// <summary>
        /// Creates the semantic handoff for an EPUB that completed repair
        /// processing without any remaining repair opportunity.
        /// </summary>
        private void CreateCompletedHandoff(
            FileContext file)
        {
            CreateHandoff(
                file,
                E_RepairHandoffStatus.RepairCompleted,
                "Repair processing completed for this EPUB.");
        }

        /// <summary>
        /// Creates or replaces the handoff for one original EPUB.
        ///
        /// OriginalFullPath is the stable identity. CurrentFullPath is the
        /// working representation that later stages may organize.
        /// </summary>
        private void CreateHandoff(
            FileContext file,
            E_RepairHandoffStatus status,
            string reason)
        {
            string originalPath =
                file.OriginalFullPath;

            string workingPath =
                string.IsNullOrWhiteSpace(file.CurrentFullPath)
                    ? originalPath
                    : file.CurrentFullPath;

            E_RepairHandoff handoff =
                new(
                    originalPath,
                    workingPath,
                    status,
                    reason);

            int existingIndex =
                _repairHandoffs.FindIndex(
                    existing =>
                        string.Equals(
                            existing.OriginalPath,
                            originalPath,
                            StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                _repairHandoffs[existingIndex] =
                    handoff;

                return;
            }

            _repairHandoffs.Add(handoff);
        }
    }
}