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
        // Repair reports are retained per EPUB instead of in one shared "last" slot.
        // OriginalFullPath is the stable branch identity, so reports for multiple
        // active books can coexist without overwriting one another.
        private readonly Dictionary<string, RepairReport> _reportsByOriginalPath =
            new(StringComparer.OrdinalIgnoreCase);

        //---------------------------------------------------------
        // Collection-level repair report
        //
        // This contains the repair opportunities for the entire
        // metadata collection. Collection-level actions such as
        // "Research Missing ISBNs" use this report.
        //---------------------------------------------------------

        private RepairReport? _collectionReport;

        // Stable collection-wide lookup for terminal user decisions.
        // RepairReport contains opportunities/evidence, not FileContext
        // records, so terminal decisions must use the expedition's original
        // FileContext set rather than inventing a Records property on the
        // repair report.
        private readonly Dictionary<string, FileContext> _filesByOriginalPath =
            new(StringComparer.OrdinalIgnoreCase);

        //---------------------------------------------------------
        // Semantic repair handoffs
        //---------------------------------------------------------

        private readonly List<E_RepairHandoff> _repairHandoffs = new();

        //---------------------------------------------------------
        // User-supplied evidence
        //---------------------------------------------------------
        //
        // User evidence is tied to OriginalFullPath, the stable identity
        // of the ebook. It survives re-observation so information supplied
        // by the user can continue to inform later repair opportunities.
        //
        //---------------------------------------------------------

        private readonly Dictionary<string, List<MetadataEvidence>>
            _userEvidenceByOriginalPath =
                new(StringComparer.OrdinalIgnoreCase);

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

        public void RevokeAutomaticRepairs()
        {
            _repairAuthorization.RevokeAutomaticRepairs();
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

            _userEvidenceByOriginalPath.Clear();

            _collectionReport = null;
            _reportsByOriginalPath.Clear();
            _filesByOriginalPath.Clear();

            foreach (FileContext file in files)
            {
                if (file == null || string.IsNullOrWhiteSpace(file.OriginalFullPath))
                    continue;

                _filesByOriginalPath[file.OriginalFullPath] = file;
            }

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
        /// each EPUB's report separately, keyed by OriginalFullPath.
        /// </summary>
        public List<ExpertFinding> Investigate(
            MetadataReport metadataReport)
        {
            ArgumentNullException.ThrowIfNull(metadataReport);

            List<ExpertFinding> findings = new();

            //---------------------------------------------------------
            // Preserve the collection-wide repair opportunities.
            //
            // This report is intentionally separate from the per-EPUB reports.
            // Collection-level actions such as ISBN research need to
            // see all applicable EPUBs, not only the current expedition
            // EPUB.
            //---------------------------------------------------------

            RepairBlock block = new();

            _collectionReport =
                block.Analyze(metadataReport);

            AttachRetainedUserEvidence(
                _collectionReport,
                metadataReport);

            ApplyIdentityEvaluations(
                _collectionReport,
                metadataReport);

            //---------------------------------------------------------
            // Activate the complete collection into branch-aware
            // expedition state before evaluating repair.
            //
            // ActiveFiles is keyed by OriginalFullPath, so a branch that
            // needs the user's attention can remain active without stopping
            // the other EPUBs from being investigated. CurrentFile remains
            // only as a compatibility cursor for older callers.
            //---------------------------------------------------------

            while (_repairExpedition.PendingCount > 0)
            {
                _repairExpedition.ActivateNext();
            }

            E_RepairConsultant consultant = new();

            //---------------------------------------------------------
            // Investigate every active branch.
            //
            // Take a snapshot because completed branches are removed from
            // the expedition during this pass. An unresolved branch remains
            // active and therefore becomes a waiting branch rather than a
            // collection-wide stop.
            //---------------------------------------------------------

            foreach (FileContext currentFile in
                     _repairExpedition.ActiveFiles.ToList())
            {
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
                // this EPUB is complete. Leave the branch active.
                //-----------------------------------------------------

                if (currentRecord == null)
                {
                    _reportsByOriginalPath[currentFile.OriginalFullPath] =
                        new RepairReport();
                    continue;
                }

                MetadataReport currentMetadataReport =
                    new();

                currentMetadataReport.Records.Add(
                    currentRecord);

                RepairReport report =
                    block.Analyze(currentMetadataReport);

                AttachRetainedUserEvidence(
                    report,
                    currentRecord);

                ApplyIdentityEvaluations(
                    report,
                    currentMetadataReport);

                //-----------------------------------------------------
                // Preserve the report for this EPUB.
                //
                // The original path is the stable branch identity.
                // Multiple EPUB reports can now coexist in the same
                // expedition.
                //-----------------------------------------------------

                _reportsByOriginalPath[currentFile.OriginalFullPath] = report;

                //-----------------------------------------------------
                // A complete EPUB requires no repair conversation.
                // Complete only this branch and continue with the rest
                // of the collection.
                //-----------------------------------------------------

                if (report.IsComplete)
                {
                    CreateCompletedHandoff(currentFile);

                    _repairExpedition.Complete(
                        currentFile.OriginalFullPath);

                    continue;
                }

                //-----------------------------------------------------
                // This branch requires attention. Record its finding but
                // deliberately do NOT return. The branch remains active
                // and is therefore represented as waiting while other
                // EPUB branches continue through investigation.
                //-----------------------------------------------------

                findings.AddRange(
                    consultant.Review(report));
            }

            //---------------------------------------------------------
            // No EPUBs remain that require repair.
            //---------------------------------------------------------

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
            CurrentFile == null
                ? Array.Empty<RepairOpportunity>()
                : GetRepairOpportunitiesFor(CurrentFile.OriginalFullPath);

        /// <summary>
        /// Returns the repair opportunities belonging to one EPUB branch.
        /// </summary>
        internal IReadOnlyList<RepairOpportunity> GetRepairOpportunitiesFor(
            string originalFullPath)
        {
            if (string.IsNullOrWhiteSpace(originalFullPath))
                return Array.Empty<RepairOpportunity>();

            return _reportsByOriginalPath.TryGetValue(
                    originalFullPath,
                    out RepairReport? report)
                ? report.Opportunities
                : Array.Empty<RepairOpportunity>();
        }

        /// <summary>
        /// Returns the original paths of EPUBs that still have active repair
        /// opportunities and have not reached a terminal repair handoff.
        ///
        /// These paths are temporarily ineligible for Organization. The user
        /// must either resolve the repair, accept the EPUB as-is, or omit it.
        /// This state remains entirely inside the Ebook Expert.
        /// </summary>
        internal IReadOnlyList<string> UnresolvedRepairPaths
        {
            get
            {
                List<string> paths = new();

                //-----------------------------------------------------
                // Eligibility must be collection-wide.
                //
                // The repair expedition deliberately investigates one EPUB
                // at a time and therefore _reportsByOriginalPath only contains
                // books that the expedition has reached. Organization cannot
                // use that dictionary to decide whether an unreached book is
                // eligible.
                //
                // _collectionReport, however, was analyzed from the complete
                // metadata collection and has already had identity evaluation
                // applied. Its opportunities therefore represent every book
                // currently known to require repair.
                //-----------------------------------------------------

                if (_collectionReport == null)
                    return paths;

                foreach (RepairOpportunity opportunity in
                         _collectionReport.Opportunities)
                {
                    string? originalPath =
                        opportunity.Record?.File?.OriginalFullPath;

                    if (string.IsNullOrWhiteSpace(originalPath))
                        continue;

                    //-------------------------------------------------
                    // A terminal handoff means the book has reached an
                    // explicit organization outcome even if the original
                    // RepairOpportunity still appears in the collection
                    // report (for example, after AcceptAsIs).
                    //-------------------------------------------------

                    bool hasTerminalHandoff =
                        _repairHandoffs.Any(
                            handoff =>
                                string.Equals(
                                    handoff.OriginalPath,
                                    originalPath,
                                    StringComparison.OrdinalIgnoreCase) &&
                                (handoff.Status == E_RepairHandoffStatus.RepairCompleted ||
                                 handoff.Status == E_RepairHandoffStatus.AcceptedAsIs ||
                                 handoff.Status == E_RepairHandoffStatus.Omitted));

                    if (!hasTerminalHandoff)
                        paths.Add(originalPath);
                }

                return paths;
            }
        }

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
            CurrentFile != null &&
            IsCompleteFor(CurrentFile.OriginalFullPath);

        public FileContext? CurrentFile =>
            _repairExpedition.CurrentFile;
        public bool ExpeditionIsComplete =>
            _repairExpedition.IsComplete;

        internal int ExpeditionTotal =>
            _repairExpedition.TotalCount;

        internal int ExpeditionCompleted =>
            _repairExpedition.CompletedFiles.Count;

        internal int ExpeditionProcessing =>
            _repairExpedition.ActiveCount;

        internal int ExpeditionPending =>
            _repairExpedition.PendingCount;

        /// <summary>
        /// Completes the specific repair branch identified by its stable
        /// original path after that EPUB has been re-observed.
        /// </summary>
        /// <summary>
        /// Re-investigates one specific EPUB after a repair action has changed
        /// its working representation.
        ///
        /// This is deliberately branch-targeted. A collection re-observation
        /// may contain several EPUBs, but the repair branch that triggered the
        /// re-observation is identified by OriginalFullPath. Keeping this
        /// decision here prevents the compatibility CurrentFile cursor from
        /// deciding which branch gets completed.
        /// </summary>
        public List<ExpertFinding> InvestigateBranch(
            MetadataReport metadataReport,
            string originalFullPath)
        {
            ArgumentNullException.ThrowIfNull(metadataReport);
            ArgumentException.ThrowIfNullOrWhiteSpace(originalFullPath);

            RepairBlock block = new();
            _collectionReport = block.Analyze(metadataReport);

            MetadataRecord? record =
                metadataReport.Records.FirstOrDefault(
                    item => string.Equals(
                        item.File?.OriginalFullPath,
                        originalFullPath,
                        StringComparison.OrdinalIgnoreCase));

            if (record == null)
            {
                _reportsByOriginalPath[originalFullPath] =
                    new RepairReport();

                return new List<ExpertFinding>();
            }

            MetadataReport branchMetadata = new();
            branchMetadata.Records.Add(record);

            RepairReport report =
                block.Analyze(branchMetadata);

            AttachRetainedUserEvidence(
                report,
                record);

            ApplyIdentityEvaluations(
                report,
                branchMetadata);

            _reportsByOriginalPath[originalFullPath] = report;

            E_RepairConsultant consultant = new();
            List<ExpertFinding> findings =
                consultant.Review(report);

            // Completion is intentionally handled by the workflow after the
            // targeted re-observation returns. Keeping the expedition state
            // active here lets ProjectWorkflow advance the branch exactly once
            // and then continue with the next pending EPUB.

            return findings;
        }

        public bool CompleteCurrentIfComplete(
            string? originalFullPath)
        {
            if (string.IsNullOrWhiteSpace(originalFullPath))
                return CompleteCurrentIfComplete();

            FileContext? branch =
                _repairExpedition.GetActive(originalFullPath);

            if (branch == null)
                return false;

            if (!IsCompleteFor(originalFullPath))
                return false;

            CreateCompletedHandoff(branch);

            return _repairExpedition.Complete(originalFullPath);
        }

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



        /// <summary>
        /// Runs identity reconciliation after ordinary missing-field analysis
        /// and before the repair report may be considered complete.
        /// </summary>
        private static void ApplyIdentityEvaluations(
            RepairReport report,
            MetadataReport metadataReport)
        {
            E_BookIdentityEvaluator evaluator = new();

            foreach (MetadataRecord record in metadataReport.Records)
            {
                BookIdentityEvaluation evaluation =
                    evaluator.Evaluate(record);

                RepairOpportunity? opportunity =
                    report.Opportunities.FirstOrDefault(
                        item => string.Equals(
                            item.Record?.File?.OriginalFullPath,
                            record.File?.OriginalFullPath,
                            StringComparison.OrdinalIgnoreCase));

                if (!evaluation.RepairRequired)
                    continue;

                if (opportunity == null)
                {
                    opportunity = new RepairOpportunity
                    {
                        Record = record
                    };

                    report.Opportunities.Add(opportunity);
                }

                opportunity.IdentityEvaluation = evaluation;
            }

            report.RepairableBooks = report.Opportunities.Count;
        }

        /// <summary>
        /// Adds user-supplied information to the stable evidence history
        /// for one ebook.
        ///
        /// The information is also attached to the currently retained
        /// repair report so the next repair action can use it immediately.
        /// </summary>
        internal bool AddUserEvidence(
            string originalFullPath,
            string value)
        {
            return AddUserFieldEvidence(
                originalFullPath,
                "Identity",
                value);
        }

        /// <summary>
        /// Retains user-provided evidence for a specific metadata field.
        ///
        /// The stable OriginalFullPath remains the branch identity, while the
        /// field name prevents a Description supplied by the user from being
        /// mistaken for Title, Series, ISBN, or generic identity evidence.
        /// </summary>
        internal bool AddUserFieldEvidence(
            string originalFullPath,
            string field,
            string value)
        {
            if (string.IsNullOrWhiteSpace(originalFullPath) ||
                string.IsNullOrWhiteSpace(field) ||
                string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string normalizedValue = value.Trim();

            if (!_userEvidenceByOriginalPath.TryGetValue(
                    originalFullPath,
                    out List<MetadataEvidence>? evidence))
            {
                evidence = new List<MetadataEvidence>();

                _userEvidenceByOriginalPath[originalFullPath] =
                    evidence;
            }

            bool alreadyRecorded =
                evidence.Any(
                    item =>
                        string.Equals(
                            item.Field,
                            field,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            item.Value?.Trim(),
                            normalizedValue,
                            StringComparison.OrdinalIgnoreCase));

            if (!alreadyRecorded)
            {
                evidence.Add(
                    new MetadataEvidence
                    {
                        Source = "User",
                        Field = field.Trim(),
                        Value = normalizedValue,
                        Location = "User-supplied repair information",
                        Notes =
                            "Information supplied by the user during repair research."
                    });
            }

            if (_reportsByOriginalPath.TryGetValue(
                    originalFullPath,
                    out RepairReport? report))
            {
                AttachRetainedUserEvidence(
                    report,
                    originalFullPath);
            }

            if (_collectionReport != null)
            {
                AttachRetainedUserEvidence(
                    _collectionReport,
                    originalFullPath);
            }

            return true;
        }

        /// <summary>
        /// Returns the user-supplied evidence retained for one ebook.
        /// </summary>
        internal IReadOnlyList<MetadataEvidence> GetUserEvidence(
            string originalFullPath)
        {
            if (string.IsNullOrWhiteSpace(originalFullPath))
                return Array.Empty<MetadataEvidence>();

            return _userEvidenceByOriginalPath.TryGetValue(
                    originalFullPath,
                    out List<MetadataEvidence>? evidence)
                ? evidence
                : Array.Empty<MetadataEvidence>();
        }

        private void AttachRetainedUserEvidence(
            RepairReport report,
            MetadataRecord record)
        {
            AttachRetainedUserEvidence(
                report,
                record.File?.OriginalFullPath ?? string.Empty);
        }

        private void AttachRetainedUserEvidence(
            RepairReport report,
            MetadataReport metadataReport)
        {
            foreach (MetadataRecord record in metadataReport.Records)
            {
                AttachRetainedUserEvidence(
                    report,
                    record);
            }
        }

        private void AttachRetainedUserEvidence(
            RepairReport report,
            string originalFullPath)
        {
            if (string.IsNullOrWhiteSpace(originalFullPath))
                return;

            if (!_userEvidenceByOriginalPath.TryGetValue(
                    originalFullPath,
                    out List<MetadataEvidence>? retainedEvidence))
            {
                return;
            }

            foreach (RepairOpportunity opportunity in report.Opportunities)
            {
                MetadataRecord? record = opportunity.Record;

                if (!string.Equals(
                        record?.File?.OriginalFullPath,
                        originalFullPath,
                        StringComparison.OrdinalIgnoreCase) ||
                    record is null)
                {
                    continue;
                }

                foreach (MetadataEvidence evidence in retainedEvidence)
                {
                    bool alreadyAttached =
                        record.Evidence.Any(
                            existing =>
                                string.Equals(
                                    existing.Source,
                                    evidence.Source,
                                    StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(
                                    existing.Value,
                                    evidence.Value,
                                    StringComparison.OrdinalIgnoreCase));

                    if (!alreadyAttached)
                    {
                        record.Evidence.Add(
                            new MetadataEvidence
                            {
                                Source = evidence.Source,
                                Field = evidence.Field,
                                Value = evidence.Value,
                                Location = evidence.Location,
                                Notes = evidence.Notes
                            });
                    }
                }
            }
        }

        /// <summary>
        /// Resolves an unresolved EPUB through an explicit user decision.
        ///
        /// This is deliberately different from the legacy deferred path:
        /// the user has made a terminal decision for this EPUB, so the branch
        /// may leave active repair processing.
        /// </summary>
        internal bool ResolveUserDecision(
            string originalFullPath,
            E_RepairHandoffStatus status,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(originalFullPath))
                return false;

            if (status != E_RepairHandoffStatus.AcceptedAsIs &&
                status != E_RepairHandoffStatus.Omitted)
            {
                throw new ArgumentException(
                    "Only AcceptedAsIs or Omitted may resolve an unresolved EPUB.",
                    nameof(status));
            }

            FileContext? file =
                _repairExpedition.GetActive(originalFullPath);

            //---------------------------------------------------------
            // Collection-wide terminal decisions must not depend on the
            // branch currently being active. The collection progress UI
            // deliberately exposes every unresolved EPUB, while the
            // expedition may still have that EPUB pending or may have
            // already moved its compatibility cursor elsewhere.
            //---------------------------------------------------------

            // A terminal decision is intentionally idempotent. The collection
            // progress UI can briefly outlive the underlying branch after a
            // click, so a repeated Accept-as-is/Omit must not turn into a
            // mysterious "no branch" failure.
            if (_repairExpedition.CompletedFiles.Contains(originalFullPath))
                return true;

            if (file == null)
            {
                _filesByOriginalPath.TryGetValue(
                    originalFullPath,
                    out file);
            }

            if (file == null)
                return false;

            CreateHandoff(file, status, reason);

            return _repairExpedition.ResolveTerminal(
                originalFullPath);
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
                GetRepairOpportunitiesFor(originalFullPath)
                    .FirstOrDefault(
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
