using System;
using System.Collections.Generic;
using SmartRenamer.Models;
using Scout.Observations.Experts.EbookExpert.Data;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization;
using Scout.Observations.Experts.EbookExpert.Investigations.Organization;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;

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
        internal OrganizationCopyResult ExecuteOne(
    OrganizationPlanEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (Job == null)
            {
                return OrganizationCopyResult.Failed(
                    "No organization job is available.");
            }

            OrganizationJobExecutor executor = new();

            return executor.Execute(
                Job,
                entry);
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
