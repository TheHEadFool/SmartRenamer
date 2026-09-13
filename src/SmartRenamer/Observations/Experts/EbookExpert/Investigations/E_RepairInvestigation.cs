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
    /// • Preserve state for each specific repair opportunity.
    /// • Produce ExpertFindings.
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
                // Synchronize the state ledger with the factual repair
                // opportunities discovered for this EPUB.
                //
                // RepairOpportunity represents the ebook as a whole
                // and contains the missing-field facts.
                //
                // RepairOpportunityState represents ONE specific
                // missing field.
                //-----------------------------------------------------

                SynchronizeOpportunityStates(report);

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
        /// Synchronizes the expedition's opportunity-state ledger with the
        /// factual repair opportunities discovered for the current EPUB.
        ///
        /// A single RepairOpportunity may contain several missing fields.
        /// Each missing field receives its own independent state.
        /// </summary>
        private void SynchronizeOpportunityStates(
            RepairReport report)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            foreach (RepairOpportunity opportunity
                in report.Opportunities)
            {
                string? originalPath =
                    opportunity.Record?.File?.OriginalFullPath;

                if (string.IsNullOrWhiteSpace(originalPath))
                    continue;

                IReadOnlyList<RepairOpportunityState> existingStates =
                    _repairExpedition.GetOpportunityStates(
                        originalPath);

                List<RepairOpportunityState> states =
                    new();

                AddOpportunityState(
                    opportunity.MissingTitle,
                    "Title",
                    existingStates,
                    states,
                    originalPath);

                AddOpportunityState(
                    opportunity.MissingAuthor,
                    "Author",
                    existingStates,
                    states,
                    originalPath);

                AddOpportunityState(
                    opportunity.MissingIsbn,
                    "ISBN",
                    existingStates,
                    states,
                    originalPath);

                AddOpportunityState(
                    opportunity.MissingPublisher,
                    "Publisher",
                    existingStates,
                    states,
                    originalPath);

                AddOpportunityState(
                    opportunity.MissingLanguage,
                    "Language",
                    existingStates,
                    states,
                    originalPath);

                AddOpportunityState(
                    opportunity.MissingDescription,
                    "Description",
                    existingStates,
                    states,
                    originalPath);

                AddOpportunityState(
                    opportunity.MissingCover,
                    "Cover",
                    existingStates,
                    states,
                    originalPath);

                _repairExpedition.SetOpportunityStates(
                    originalPath,
                    states);
            }
        }

        /// <summary>
        /// Adds a state for one specific repair type when that repair
        /// opportunity is factually present.
        ///
        /// Existing state is preserved so that research, decisions, and
        /// deferrals survive subsequent observations of the same EPUB.
        /// </summary>
        private static void AddOpportunityState(
            bool isMissing,
            string repairType,
            IReadOnlyList<RepairOpportunityState> existingStates,
            List<RepairOpportunityState> states,
            string originalPath)
        {
            if (!isMissing)
                return;

            RepairOpportunityState? existingState =
                existingStates.FirstOrDefault(
                    state =>
                        string.Equals(
                            state.RepairType,
                            repairType,
                            StringComparison.OrdinalIgnoreCase));

            if (existingState != null)
            {
                states.Add(existingState);
                return;
            }

            states.Add(
                new RepairOpportunityState(
                    originalPath,
                    repairType));
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

        public double CurrentConfidenceThreshold =>
            _repairExpedition.CurrentConfidenceThreshold;

        /// <summary>
        /// Lowers the confidence threshold for the active repair expedition
        /// by one policy step.
        ///
        /// The expedition owns the threshold policy and enforces its minimum.
        /// This investigation exposes that policy to the Ebook Expert
        /// coordinator without exposing the expedition itself.
        /// </summary>
        public double LowerConfidenceThreshold()
        {
            return _repairExpedition.LowerConfidenceThreshold();
        }

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