using Scout.Observations.Experts.EbookExpert.Data;
using Scout.Observations.Experts.EbookExpert.Investigations.Organization;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        /// <summary>
        /// The organization context for the current Ebook expedition.
        /// This remains null until an organization report has been produced.
        /// </summary>
        internal OrganizationContext? Context { get; private set; }

        /// <summary>
        /// The collection-level organization choices currently supplied to
        /// the Ebook Expert. These choices are configuration only; they do
        /// not authorize filesystem changes.
        /// </summary>
        public OrganizationOptions Options { get; private set; } = new();

        /// <summary>
        /// The current collection-level destination plan.
        ///
        /// The plan contains no filesystem operations. It is the durable map
        /// from stable ebook identity to the destination chosen by the current
        /// organization configuration.
        /// </summary>
        public OrganizationPlan Plan { get; private set; } = new();

        /// <summary>
        /// Latest generic progress snapshot for the collection.
        /// This lets later Ebook Expert stages preserve the dashboard state
        /// without exposing Organization types to the UI.
        /// </summary>
        public IReadOnlyList<ExecutionProgressItem> ProgressItems { get; private set; } =
            Array.Empty<ExecutionProgressItem>();

        /// <summary>
        /// The current collection-level organization job.
        ///
        /// The job connects planned identities with their observed book
        /// snapshots without materializing execution operations.
        ///
        /// The job performs no filesystem work.
        /// </summary>
        internal OrganizationJob? Job { get; private set; }

        /// <summary>
        /// The collection metadata used to build the organization book
        /// snapshots. This is retained only for the current investigation.
        /// </summary>
        private MetadataReport? _metadataReport;

        /// <summary>
        /// Semantic results supplied by the Ebook Expert repair stage.
        /// </summary>
        internal IReadOnlyList<E_RepairHandoff> RepairHandoffs { get; private set; } =
            Array.Empty<E_RepairHandoff>();

        /// <summary>
        /// Original paths whose repair work is unresolved and therefore not
        /// yet eligible for Organization.
        /// </summary>
        private IReadOnlyList<string> UnresolvedRepairPaths { get; set; } =
            Array.Empty<string>();

        /// <summary>
        /// Successful organization results retained for the lifetime of the
        /// current Ebook expedition. The key is the immutable OriginalPath.
        ///
        /// This is execution history, not organization planning state.
        /// </summary>
        private readonly Dictionary<string, string> _organizedDestinations =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Indicates whether this expedition has already successfully
        /// organized the ebook identified by OriginalPath.
        /// </summary>
        internal bool IsOrganized(string originalPath)
        {
            if (string.IsNullOrWhiteSpace(originalPath))
                return false;

            lock (_organizedDestinations)
            {
                return _organizedDestinations.ContainsKey(originalPath);
            }
        }

        /// <summary>
        /// Supplies the collection-level organization choices that will be
        /// combined with the next organization report.
        /// </summary>
        public void ConfigureOptions(OrganizationOptions options)
        {
            Options =
                options ??
                throw new System.ArgumentNullException(nameof(options));

            if (Report != null)
            {
                Context =
                    new OrganizationContext(
                        Report,
                        Options,
                        RepairHandoffs,
                        BuildBooks());

                Plan = BuildPlan();
                Job = BuildJob();
            }
        }

        /// <summary>
        /// Supplies semantic repair results to the organization stage.
        ///
        /// This is a handoff of working representations only. It does not
        /// execute organization or authorize filesystem changes.
        /// </summary>
        internal void AcceptRepairHandoffs(
            IReadOnlyList<E_RepairHandoff> repairHandoffs,
            IReadOnlyList<string>? unresolvedRepairPaths = null)
        {
            RepairHandoffs =
                repairHandoffs ??
                throw new System.ArgumentNullException(nameof(repairHandoffs));

            UnresolvedRepairPaths =
                unresolvedRepairPaths ??
                Array.Empty<string>();

            if (Report != null)
            {
                Context =
                    new OrganizationContext(
                        Report,
                        Options,
                        RepairHandoffs,
                        BuildBooks());

                Plan = BuildPlan();
                Job = BuildJob();
            }
        }

        /// <summary>
        /// Creates passive per-book organization snapshots from the metadata
        /// already gathered by the Ebook Expert.
        ///
        /// A repair handoff supplies the current working representation when
        /// one exists. No ebook is opened or modified here.
        /// </summary>
        private IReadOnlyList<OrganizationBook> BuildBooks()
        {
            if (_metadataReport == null)
                return Array.Empty<OrganizationBook>();

            Dictionary<string, E_RepairHandoff> handoffs =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (E_RepairHandoff handoff in RepairHandoffs)
            {
                if (handoff.Status == E_RepairHandoffStatus.Omitted)
                    continue;

                handoffs[handoff.OriginalPath] = handoff;
            }

            List<OrganizationBook> books = new();

            foreach (MetadataRecord record in _metadataReport.Records)
            {
                if (record == null ||
                    record.File == null ||
                    record.Metadata == null)
                {
                    continue;
                }

                string originalPath =
                    record.File.OriginalFullPath;

                if (UnresolvedRepairPaths.Any(
                        path =>
                            string.Equals(
                                path,
                                originalPath,
                                StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                string workingPath =
                    record.File.CurrentFullPath;

                if (handoffs.TryGetValue(
                        originalPath,
                        out E_RepairHandoff? handoff))
                {
                    workingPath =
                        handoff.WorkingPath;
                }

                string organizationSeries =
                    GetOrganizationSeries(
                        record,
                        handoff);

                books.Add(
                    new OrganizationBook
                    {
                        OriginalPath = originalPath,
                        WorkingPath = workingPath,
                        Title = record.Metadata.Title,
                        Author = GetPrimaryAuthor(record.Metadata.Author),
                        Series = organizationSeries,
                        SeriesNumber = GetOrganizationSeriesNumber(
                            record,
                            handoff),
                        Publisher = record.Metadata.Publisher,
                        Isbn = record.Metadata.Isbn,
                        Language = record.Metadata.Language
                    });
            }

            return books;
        }

        /// <summary>
        /// Determines the series identity that Organization should use.
        ///
        /// Organization must not blindly route a book using malformed or
        /// composite raw Series metadata when the Ebook Expert has already
        /// resolved a better local identity. The raw metadata remains intact
        /// as observation/evidence; this method only selects the value for
        /// organization routing.
        ///
        /// A terminal AcceptAsIs decision is respected: in that case the user
        /// explicitly chose to organize the observed ebook without applying
        /// the unresolved repair candidate.
        /// </summary>
        private static string GetOrganizationSeries(
            MetadataRecord record,
            E_RepairHandoff? handoff)
        {
            string observedSeries =
                record.Metadata.Series?.Trim() ?? string.Empty;

            if (handoff?.Status == E_RepairHandoffStatus.AcceptedAsIs)
                return observedSeries;

            E_BookIdentityEvaluator evaluator =
                new();

            BookIdentityEvaluation evaluation =
                evaluator.Evaluate(record);

            if (evaluation.SeriesEvaluation?.State ==
                    SeriesEvidenceState.Resolved &&
                !string.IsNullOrWhiteSpace(
                    evaluation.SeriesEvaluation.CandidateSeries))
            {
                return evaluation.SeriesEvaluation.CandidateSeries.Trim();
            }

            return observedSeries;
        }

        /// <summary>
        /// Selects the primary author used by Organization from the author
        /// information supplied by the Ebook metadata reader.
        ///
        /// The metadata reader preserves multiple creators in their source
        /// order using a semicolon-separated value. For the current
        /// organization slice, Scout treats the first listed creator as the
        /// primary author.
        ///
        /// This is deliberately kept at the organization-domain boundary.
        /// The complete creator information remains available in the metadata
        /// model for future enrichment/research work.
        /// </summary>
        private static string GetOrganizationSeriesNumber(
            MetadataRecord record,
            E_RepairHandoff? handoff)
        {
            string observedNumber =
                record.Metadata.SeriesNumber?.Trim() ?? string.Empty;

            if (handoff?.Status == E_RepairHandoffStatus.AcceptedAsIs)
                return observedNumber;

            E_BookIdentityEvaluator evaluator =
                new();

            BookIdentityEvaluation evaluation =
                evaluator.Evaluate(record);

            if (evaluation.SeriesEvaluation?.State ==
                    SeriesEvidenceState.Resolved &&
                !string.IsNullOrWhiteSpace(
                    evaluation.SeriesEvaluation.CandidateSeriesNumber))
            {
                return evaluation.SeriesEvaluation.CandidateSeriesNumber.Trim();
            }

            return observedNumber;
        }

        private static string GetPrimaryAuthor(string authorValue)
        {
            if (string.IsNullOrWhiteSpace(authorValue))
                return string.Empty;

            string[] authors =
                authorValue.Split(
                    ';',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries);

            return authors.Length > 0
                ? authors[0]
                : authorValue.Trim();
        }

        private OrganizationPlan BuildPlan()
        {
            if (Context == null)
                return new OrganizationPlan();

            OrganizationPlanBuilder builder = new();

            return builder.Build(Context);
        }

        private OrganizationJob BuildJob()
        {
            if (Context == null)
            {
                return new OrganizationJob(
                    Plan,
                    Options,
                    Array.Empty<OrganizationBook>());
            }

            return new OrganizationJob(
                Plan,
                Options,
                Context.Books);
        }

        /// <summary>
        /// Executes one already-planned organization entry through the
        /// current collection-level organization job.
        ///
        /// A book does not require a RepairHandoff merely to be organized.
        /// Books that required repair may have a handoff, in which case
        /// BuildBooks() supplies the repaired WorkingPath. Books that did not
        /// require repair use their existing FileContext.CurrentFullPath.
        ///
        /// This distinction is important: RepairHandoff represents a semantic
        /// handoff from the Repair stage; it is not a universal prerequisite
        /// for Organization.
        ///
        /// Successful execution is retained as expedition execution history.
        ///
        /// This method does not:
        /// • create a queue
        /// • create multiple operations
        /// • release the working representation
        /// • alter the original EPUB
        /// • decide collection-level concurrency policy
        /// </summary>
        internal OrganizationCopyResult ExecuteOne(
            OrganizationPlanEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (Job == null)
            {
                return OrganizationCopyResult.Failed(
                    "No organization job is available.");
            }

            //-------------------------------------------------------------
            // IMPORTANT
            //
            // Organization does NOT require a RepairHandoff.
            //
            // A book that never needed repair is already a valid
            // organization candidate. Its current representation comes
            // directly from FileContext.CurrentFullPath through BuildBooks().
            //
            // A repaired book may have a RepairHandoff, and BuildBooks()
            // will use the handoff's WorkingPath.
            //
            // Readiness that depends on an unresolved Repair decision must
            // eventually be represented by the broader EbookExpert lifecycle
            // rather than by assuming every book must possess a handoff.
            //
            // Do not put the old blanket RepairHandoff gate back here.
            //-------------------------------------------------------------

            OrganizationJobExecutor executor = new();

            OrganizationCopyResult result =
                executor.Execute(
                    Job,
                    entry);

            if (result.Succeeded &&
                !string.IsNullOrWhiteSpace(result.DestinationPath))
            {
                lock (_organizedDestinations)
                {
                    _organizedDestinations[entry.OriginalPath] =
                        result.DestinationPath;
                }
            }

            return result;
        }

        /// <summary>
        /// Executes every currently planned organization entry that has not
        /// already completed successfully during this expedition.
        ///
        /// Organization execution is independent per planned ebook. The
        /// investigation therefore uses bounded concurrency here rather than
        /// forcing the collection through one ebook at a time.
        ///
        /// IMPORTANT: this is deliberately bounded. We do not create one
        /// filesystem task per book without limit. The organization operation
        /// already has a per-book working representation and destination, so
        /// a small worker limit lets independent books progress together
        /// without turning a large collection into an uncontrolled I/O burst.
        ///
        /// Successful execution remains recorded in the investigation ledger,
        /// not in OrganizationPlanEntry.
        /// </summary>
        private IReadOnlyList<ExecutionProgressItem> BuildProgressItems(
            IReadOnlyList<OrganizationPlanEntry> entries,
            IReadOnlyDictionary<string, ExecutionProgressItem> states)
        {
            Dictionary<string, ExecutionProgressItem> items =
                new(StringComparer.OrdinalIgnoreCase);

            // First add every book that is actually in the organization plan.
            // These entries may be Pending, Processing, Completed, or Failed.
            foreach (OrganizationPlanEntry entry in entries)
            {
                if (states.TryGetValue(
                        entry.OriginalPath,
                        out ExecutionProgressItem? item))
                {
                    items[entry.OriginalPath] = item;
                    continue;
                }

                items[entry.OriginalPath] =
                    new ExecutionProgressItem
                    {
                        Key = entry.OriginalPath,
                        DisplayName = entry.DestinationFileName,
                        State = "Pending",
                        Status = "Pending",
                        Completed = 0,
                        Total = 1
                    };
            }

            // The organization plan is deliberately a subset of the collection.
            // A book excluded because its repair is unresolved must NOT disappear
            // from the collection progress window. It remains physically
            // unorganized and therefore needs an explicit Unorganized row.
            //
            // This is presentation state only. It does not make the book eligible
            // for organization and does not change BuildBooks() or the safety
            // boundary around unresolved repairs.
            if (_metadataReport != null)
            {
                HashSet<string> plannedPaths =
                    new(
                        entries.Select(entry => entry.OriginalPath),
                        StringComparer.OrdinalIgnoreCase);

                foreach (MetadataRecord record in _metadataReport.Records)
                {
                    if (record?.File == null)
                        continue;

                    string originalPath =
                        record.File.OriginalFullPath;

                    if (plannedPaths.Contains(originalPath))
                        continue;

                    bool waiting =
                        UnresolvedRepairPaths.Any(
                            path => string.Equals(
                                path,
                                originalPath,
                                StringComparison.OrdinalIgnoreCase));

                    string status =
                        waiting
                            ? "Waiting for repair or decision."
                            : RepairHandoffs.Any(
                                handoff =>
                                    string.Equals(
                                        handoff.OriginalPath,
                                        originalPath,
                                        StringComparison.OrdinalIgnoreCase) &&
                                    handoff.Status == E_RepairHandoffStatus.Omitted)
                                ? "Omitted."
                                : "Not eligible for organization.";

                    IReadOnlyList<ExecutionProgressAction> actions =
                        waiting
                            ? new[]
                            {
                                new ExecutionProgressAction
                                {
                                    Id = $"EditInformation:{originalPath}",
                                    Label = "Edit / add information",
                                    ActionId = "AddRepairInformation",
                                    ContextId = originalPath
                                },
                                new ExecutionProgressAction
                                {
                                    Id = $"AcceptAsIs:{originalPath}",
                                    Label = "Accept as-is and organize",
                                    ActionId = "AcceptAsIs",
                                    ContextId = originalPath
                                },
                                new ExecutionProgressAction
                                {
                                    Id = $"OmitEbook:{originalPath}",
                                    Label = "Omit",
                                    ActionId = "OmitEbook",
                                    ContextId = originalPath
                                }
                            }
                            : Array.Empty<ExecutionProgressAction>();

                    items[originalPath] =
                        new ExecutionProgressItem
                        {
                            Key = originalPath,
                            DisplayName = record.File.CurrentName,
                            State = "Unorganized",
                            Status = status,
                            Completed = 0,
                            Total = 1,
                            Actions = actions
                        };
                }
            }

            return items.Values
                .OrderBy(item =>
                    item.NeedsUserAttention ? 0 :
                    string.Equals(item.State, "Processing", StringComparison.OrdinalIgnoreCase) ? 1 :
                    string.Equals(item.State, "Ready", StringComparison.OrdinalIgnoreCase) ? 2 :
                    item.IsCompleted ? 4 :
                    3)
                .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        internal IReadOnlyList<OrganizationCopyResult> ExecuteAll(
            IProgress<ExecutionProgress>? progress = null)
        {
            if (Job == null)
            {
                return new[]
                {
                    OrganizationCopyResult.Failed(
                        "No organization job is available.")
                };
            }

            const int maxConcurrentOrganizations = 6;

            int collectionTotal =
                _metadataReport?.Records.Count(record => record?.File != null)
                ?? Plan.Entries.Count;

            int collectionWaiting =
                UnresolvedRepairPaths.Count;

            int total = Plan.Entries.Count;
            int alreadyCompleted =
                Plan.Entries.Count(entry => IsOrganized(entry.OriginalPath));

            List<OrganizationPlanEntry> entries =
                Plan.Entries
                    .Where(entry =>
                        entry != null &&
                        !IsOrganized(entry.OriginalPath))
                    .ToList();

            Dictionary<string, ExecutionProgressItem> itemStates =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (OrganizationPlanEntry entry in Plan.Entries)
            {
                bool completed = IsOrganized(entry.OriginalPath);

                itemStates[entry.OriginalPath] =
                    new ExecutionProgressItem
                    {
                        Key = entry.OriginalPath,
                        DisplayName = entry.DestinationFileName,
                        State = completed ? "Completed" : "Pending",
                        Status = completed ? "Completed" : "Pending",
                        Completed = completed ? 1 : 0,
                        Total = 1
                    };
            }

            if (entries.Count == 0)
            {
                ProgressItems = BuildProgressItems(Plan.Entries, itemStates);

                progress?.Report(
                    new ExecutionProgress
                    {
                        Completed = total,
                        Total = total,
                        Stage = "Organization",
                        StageCompleted = total,
                        StageTotal = total,
                        CollectionTotal = collectionTotal,
                        CollectionCompleted = total,
                        CollectionProcessing = 0,
                        CollectionWaiting = collectionWaiting,
                        CollectionPending = 0,
                        Items = ProgressItems,
                        Status = "Organization complete."
                    });

                return Array.Empty<OrganizationCopyResult>();
            }

            ProgressItems = BuildProgressItems(Plan.Entries, itemStates);

            progress?.Report(
                new ExecutionProgress
                {
                    Completed = alreadyCompleted,
                    Total = total,
                    CurrentFile = entries[0].OriginalPath,
                    Stage = "Organization",
                    StageCompleted = alreadyCompleted,
                    StageTotal = total,
                    CollectionTotal = collectionTotal,
                    CollectionCompleted = alreadyCompleted,
                    CollectionProcessing = 0,
                    CollectionWaiting = collectionWaiting,
                    CollectionPending = entries.Count,
                    Items = ProgressItems,
                    Status = "Organization starting..."
                });

            int started = 0;
            int newlyCompleted = 0;

            using SemaphoreSlim gate =
                new(maxConcurrentOrganizations);

            Task<OrganizationCopyResult>[] tasks =
                entries.Select(async entry =>
                {
                    await gate.WaitAsync().ConfigureAwait(false);

                    int processing =
                        Interlocked.Increment(ref started) -
                        Volatile.Read(ref newlyCompleted);

                    lock (itemStates)
                    {
                        itemStates[entry.OriginalPath] =
                            new ExecutionProgressItem
                            {
                                Key = entry.OriginalPath,
                                DisplayName = entry.DestinationFileName,
                                State = "Processing",
                                Status = "Processing",
                                Completed = 0,
                                Total = 1
                            };
                    }

                    IReadOnlyList<ExecutionProgressItem> processingItems;
                    lock (itemStates)
                    {
                        processingItems = BuildProgressItems(Plan.Entries, itemStates);
                        ProgressItems = processingItems;
                    }

                    progress?.Report(
                        new ExecutionProgress
                        {
                            Completed = alreadyCompleted + Volatile.Read(ref newlyCompleted),
                            Total = total,
                            CurrentFile = entry.OriginalPath,
                            Stage = "Organization",
                            StageCompleted = alreadyCompleted + Volatile.Read(ref newlyCompleted),
                            StageTotal = total,
                            CollectionTotal = collectionTotal,
                            CollectionCompleted = alreadyCompleted + Volatile.Read(ref newlyCompleted),
                            CollectionProcessing = processing,
                            CollectionWaiting = collectionWaiting,
                            CollectionPending = Math.Max(0, total - alreadyCompleted - Volatile.Read(ref started)),
                            Items = processingItems,
                            Status = $"Organizing {entry.DestinationFileName}..."
                        });

                    try
                    {
                        OrganizationCopyResult result = ExecuteOne(entry);

                        if (result.Succeeded)
                            Interlocked.Increment(ref newlyCompleted);

                        int completed =
                            alreadyCompleted + Volatile.Read(ref newlyCompleted);

                        int currentProcessing =
                            Math.Max(0, Volatile.Read(ref started) - completed + alreadyCompleted);

                        lock (itemStates)
                        {
                            itemStates[entry.OriginalPath] =
                                new ExecutionProgressItem
                                {
                                    Key = entry.OriginalPath,
                                    DisplayName = entry.DestinationFileName,
                                    State = result.Succeeded ? "Completed" : "Unorganized",
                                    Status = result.Succeeded
                                        ? "Completed"
                                        : "Organization failed.",
                                    Completed = result.Succeeded ? 1 : 0,
                                    Total = 1
                                };
                        }

                        IReadOnlyList<ExecutionProgressItem> completedItems;
                        lock (itemStates)
                        {
                            completedItems = BuildProgressItems(Plan.Entries, itemStates);
                            ProgressItems = completedItems;
                        }

                        progress?.Report(
                            new ExecutionProgress
                            {
                                Completed = completed,
                                Total = total,
                                CurrentFile = entry.OriginalPath,
                                Stage = "Organization",
                                StageCompleted = completed,
                                StageTotal = total,
                                CollectionTotal = collectionTotal,
                                CollectionCompleted = completed,
                                CollectionProcessing = currentProcessing,
                                CollectionWaiting = collectionWaiting,
                                CollectionPending = Math.Max(0, total - completed - currentProcessing),
                                Items = completedItems,
                                Status = result.Succeeded
                                    ? $"Organized {entry.DestinationFileName}."
                                    : $"Organization could not complete {entry.DestinationFileName}."
                            });

                        return result;
                    }
                    finally
                    {
                        gate.Release();
                    }
                }).ToArray();

            Task.WaitAll(tasks);

            int finalCompleted =
                alreadyCompleted + Volatile.Read(ref newlyCompleted);

            ProgressItems = BuildProgressItems(Plan.Entries, itemStates);

            progress?.Report(
                new ExecutionProgress
                {
                    Completed = finalCompleted,
                    Total = total,
                    CurrentFile = entries[^1].OriginalPath,
                    Stage = "Organization",
                    StageCompleted = finalCompleted,
                    StageTotal = total,
                    CollectionTotal = collectionTotal,
                    CollectionCompleted = finalCompleted,
                    CollectionProcessing = 0,
                    CollectionWaiting = collectionWaiting,
                    CollectionPending = Math.Max(0, total - finalCompleted),
                    Items = ProgressItems,
                    Status = finalCompleted == total
                        ? "Organization complete."
                        : "Organization finished with pending work."
                });

            return tasks
                .Select(task => task.Result)
                .ToList();
        }

        public List<ExpertFinding> Investigate(
            MetadataReport metadataReport)

        // Begin Investigate()
        {
            List<ExpertFinding> findings = new();

            _metadataReport =
                metadataReport ??
                throw new System.ArgumentNullException(nameof(metadataReport));

            //---------------------------------------------------------
            // Ask the Block to discover facts.
            //---------------------------------------------------------

            OrganizationBlock block = new();

            Report =
                block.Analyze(metadataReport);

            Context =
                new OrganizationContext(
                    Report,
                    Options,
                    RepairHandoffs,
                    BuildBooks());

            Plan = BuildPlan();
            Job = BuildJob();

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