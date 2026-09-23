using Scout.Observations.Experts.EbookExpert.Data;
using Scout.Observations.Experts.EbookExpert.Investigations.Organization;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;
using System;
using System.Collections.Generic;

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

            return _organizedDestinations.ContainsKey(originalPath);
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
            IReadOnlyList<E_RepairHandoff> repairHandoffs)
        {
            RepairHandoffs =
                repairHandoffs ??
                throw new System.ArgumentNullException(nameof(repairHandoffs));

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

                string workingPath =
                    record.File.CurrentFullPath;

                if (handoffs.TryGetValue(
                        originalPath,
                        out E_RepairHandoff? handoff))
                {
                    workingPath =
                        handoff.WorkingPath;
                }

                books.Add(
                    new OrganizationBook
                    {
                        OriginalPath = originalPath,
                        WorkingPath = workingPath,
                        Title = record.Metadata.Title,
                        Author = record.Metadata.Author,
                        Series = record.Metadata.Series,
                        Publisher = record.Metadata.Publisher,
                        Isbn = record.Metadata.Isbn,
                        Language = record.Metadata.Language
                    });
            }

            return books;
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
        /// Successful execution is retained as expedition execution history.
        ///
        /// This method does not:
        /// • create a queue
        /// • create multiple operations
        /// • release the working representation
        /// • alter the original EPUB
        /// • manage concurrency
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

            // -----------------------------------------------------------------
            // Organization plans the entire collection, including books that
            // are not ready yet. Readiness is an execution concern, not a
            // planning concern. Repair owns the readiness signal and hands it
            // downstream through E_RepairHandoff.
            //
            // Pending is deliberately distinct from Failed: a pending book is
            // valid work that simply cannot execute yet. This allows a future
            // collection-level Organize All commitment to revisit it after the
            // repair/user-decision state changes.
            // -----------------------------------------------------------------
            if (!IsRepairCompleted(entry.OriginalPath))
            {
                return OrganizationCopyResult.Pending(
                    "This ebook is not ready for organization yet.");
            }

            OrganizationJobExecutor executor = new();

            OrganizationCopyResult result =
                executor.Execute(
                    Job,
                    entry);

            if (result.Succeeded &&
                !string.IsNullOrWhiteSpace(result.DestinationPath))
            {
                _organizedDestinations[entry.OriginalPath] =
                    result.DestinationPath;
            }

            return result;
        }

        /// <summary>
        /// Determines whether Repair has handed Organization a completed
        /// working representation for the planned ebook.
        ///
        /// Organization does not inspect Repair internals or infer readiness
        /// from the presence of a file. Repair is the owner of that decision.
        /// </summary>
        private bool IsRepairCompleted(string originalPath)
        {
            if (string.IsNullOrWhiteSpace(originalPath))
                return false;

            foreach (E_RepairHandoff handoff in RepairHandoffs)
            {
                if (string.Equals(
                        handoff.OriginalPath,
                        originalPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return handoff.Status ==
                           E_RepairHandoffStatus.RepairCompleted;
                }
            }

            return false;
        }

        /// <summary>
        /// Executes every currently planned organization entry that has not
        /// already completed successfully during this expedition.
        ///
        /// This is the collection execution primitive for Organization.
        ///
        /// It deliberately remains sequential for now. The purpose of this
        /// method is to establish collection-level execution above ExecuteOne()
        /// without introducing concurrency or UI orchestration prematurely.
        ///
        /// Entries that have already succeeded are skipped using the
        /// expedition execution ledger rather than plan state.
        /// </summary>
        internal IReadOnlyList<OrganizationCopyResult> ExecuteAll()
        {
            List<OrganizationCopyResult> results = new();

            if (Job == null)
            {
                results.Add(
                    OrganizationCopyResult.Failed(
                        "No organization job is available."));

                return results;
            }

            foreach (OrganizationPlanEntry entry in Plan.Entries)
            {
                if (entry == null)
                    continue;

                if (IsOrganized(entry.OriginalPath))
                    continue;

                OrganizationCopyResult result =
                    ExecuteOne(entry);

                results.Add(result);
            }

            return results;
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