using Scout.Observations.Conversation;
using Scout.Observations.Experts.EbookExpert.Investigations.Organization;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Action;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;
using SmartRenamer.Observations.Experts.EbookExpert.Translators;
using SmartRenamer.Observations.Specialists;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SmartRenamer.Observations
{
    // =========================================================================
    // PROJECT STATUS
    // =========================================================================
    //
    // WHY THIS CLASS EXISTS
    // -------------------------------------------------------------------------
    // The EbookExpert is Scout's reference implementation of a complete
    // Observation Expert. It demonstrates the architecture that every future
    // Expert should follow.
    //
    // It coordinates Investigations, produces ExpertFindings, and translates
    // those findings into conversation-ready recommendations that Scout can
    // present to the user.
    //
    // CURRENT MILESTONE
    // -------------------------------------------------------------------------
    // Display Ebook Expert recommendations in the existing UI.
    //
    // CURRENT STATUS
    // -------------------------------------------------------------------------
    // ✓ Observation architecture complete
    // ✓ Investigation pipeline complete
    // ✓ Report pipeline complete
    // ✓ Consultant pipeline complete
    // ✓ ExpertFinding pipeline complete
    // ✓ Recommendation translation implemented
    // ☐ Recommendation pipeline connected to existing UI
    // ☐ Conversation integration complete
    //
    // CURRENTLY DRIVING
    // -------------------------------------------------------------------------
    // The Recommendation panel (left side of the UI).
    //
    // The information produced here will determine:
    //
    // • Which recommendation buttons appear.
    // • Which actions Scout can perform immediately.
    // • Which conversation topics Scout can introduce.
    //
    // Recommendation buttons and Scout's conversation always represent the
    // same underlying understanding of the user's project.
    //
    // DO NOT CHANGE UNTIL
    // -------------------------------------------------------------------------
    // The Ebook Expert is successfully driving the Recommendation panel.
    //
    // Avoid adding new architecture or renaming classes until the current UI
    // demonstrates what information the Expert already provides.
    //
    // EXPERT FACTORY
    // -------------------------------------------------------------------------
    // YES
    //
    // This class is intended to become the template for future Experts.
    //
    // When complete, Scout should be able to generate an Expert with this
    // structure from an interview and a ChatGPT Knowledge Package.
    //
    // NEXT STEP
    // -------------------------------------------------------------------------
    // Connect the translated recommendations produced by this Expert to the
    // existing Recommendation panel without replacing the current UI.
    // =========================================================================
    public sealed class EbookExpert
        : ObservationExpert
    {
        // Set only for the observation pass immediately following a domain
        // action. The generic Observation Framework transports the identity;
        // EbookExpert uses it to target the correct repair branch.
        private string? _reobservationTargetOriginalFullPath;

        // Retained for the lifetime of the current expedition so domain
        // actions such as organization can report through the same UI
        // progress channel that carried the investigation.
        private IProgress<ExecutionProgress>? _activeProgress;

        /// <summary>
        /// Initializes Ebook Expert's domain services.
        ///
        /// Repair workspace cleanup belongs to Ebook Expert because the
        /// temporary workspace is an Ebook Expert implementation detail.
        /// Scout does not need to know how that cleanup works.
        /// </summary>
        public EbookExpert()
        {
            E_RepairWorkspace.CleanupAbandonedWorkspaces();
        }

        //---------------------------------------------------------
        // Discovery Capability
        //---------------------------------------------------------
        //
        // The Ebook Expert declares what Scout should make available
        // as candidate files during discovery.
        //
        // IMPORTANT
        // ---------------------------------------------------------
        // These extensions are candidate hints only.
        //
        // They do NOT mean that every matching file is an ebook.
        // Ebook Expert remains responsible for domain classification.
        //
        // .epub is the first candidate format because it is the format
        // the current Ebook Expert demonstrably understands end-to-end.
        //
        //---------------------------------------------------------

        private static readonly ExpertDiscoveryRequest _discoveryRequest =
            new()
            {
                CandidateExtensions =
                [
                    ".epub"
                ],
                Options =
                [
                    new ExpertDiscoveryOption(
                        "search-nested-folders",
                        "Search nested folders"),
                    new ExpertDiscoveryOption(
                        "current-folder-only",
                        "Current folder only")
                ]
            };

        public override ExpertDiscoveryRequest? DiscoveryRequest =>
            _discoveryRequest;

        //---------------------------------------------------------
        // Ebook discovery scope
        //---------------------------------------------------------
        //
        // This is an Ebook Expert domain decision.
        //
        // Scout may provide the files it discovered, but Ebook Expert
        // determines whether EPUBs in nested folders belong to this
        // Ebook expedition.
        //
        // 2A establishes the domain setting.
        // 2B will connect the user's conversation choice to this setting.
        //
        // The current default is true so existing behavior is preserved.
        //
        //---------------------------------------------------------

        public bool SearchNestedFolders { get; set; } = true;

        public override void ApplyDiscoveryChoice(string optionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(optionId);

            switch (optionId)
            {
                case "search-nested-folders":
                    SearchNestedFolders = true;
                    return;

                case "current-folder-only":
                    SearchNestedFolders = false;
                    return;

                default:
                    throw new ArgumentException(
                        $"Unknown ebook discovery option '{optionId}'.",
                        nameof(optionId));
            }
        }

        //---------------------------------------------------------
        // Organization Decision Capability
        //---------------------------------------------------------
        //
        // Organization is a collection-level configuration decision.
        //
        // It is deliberately separate from the Action framework used
        // for operational actions such as ISBN research.
        //
        // The OrganizationPathBuilder derives viable paths from the
        // current OrganizationReport. The generic Scout decision
        // infrastructure transports those choices to the user.
        //
        //---------------------------------------------------------

        private readonly OrganizationPathBuilder _organizationPathBuilder =
            new();

        private IReadOnlyList<OrganizationPathOption> _organizationPathOptions =
            Array.Empty<OrganizationPathOption>();

        /// <summary>
        /// Indicates that the collection-level organization destination has
        /// been explicitly accepted or supplied by the user. The automatically
        /// derived _Organized sibling path remains only a proposal until this
        /// becomes true.
        /// </summary>
        private bool _organizationDestinationConfirmed;

        /// <summary>
        /// True once the user has committed this ebook collection to the
        /// selected organization policy. The organization plan and job may
        /// be rebuilt after re-observation, but this commitment survives
        /// those rebuilds because EbookExpert is long-lived for the expedition.
        /// </summary>
        private bool _organizationCommitted;

        /// <summary>
        /// Domain-owned checkpoints for reversible collection-level
        /// organization decisions. The generic Guide knows only that the
        /// Expert can rewind; the Expert owns the actual state restoration.
        /// </summary>
        private readonly Stack<OrganizationDecisionSnapshot> _organizationDecisionHistory =
            new();

        private sealed class OrganizationDecisionSnapshot
        {
            public bool DestinationConfirmed { get; init; }

            public bool Committed { get; init; }

            public OrganizationOptions Options { get; init; } = new();
        }

        public override bool CanRewindDecision =>
            _organizationDecisionHistory.Count > 0;

        public override void RewindDecision()
        {
            if (_organizationDecisionHistory.Count == 0)
                return;

            OrganizationDecisionSnapshot snapshot =
                _organizationDecisionHistory.Pop();

            _organizationDestinationConfirmed =
                snapshot.DestinationConfirmed;

            _organizationCommitted =
                snapshot.Committed;

            _organizationInvestigation.ConfigureOptions(
                new OrganizationOptions
                {
                    FolderLevels =
                        new List<OrganizationDimension>(
                            snapshot.Options.FolderLevels),

                    FileOrderBy =
                        snapshot.Options.FileOrderBy,

                    FileOrderDirection =
                        snapshot.Options.FileOrderDirection,

                    DestinationRoot =
                        snapshot.Options.DestinationRoot
                });
        }

        private void SaveOrganizationDecisionCheckpoint()
        {
            OrganizationOptions options =
                _organizationInvestigation.Options;

            _organizationDecisionHistory.Push(
                new OrganizationDecisionSnapshot
                {
                    DestinationConfirmed =
                        _organizationDestinationConfirmed,

                    Committed =
                        _organizationCommitted,

                    Options =
                        new OrganizationOptions
                        {
                            FolderLevels =
                                new List<OrganizationDimension>(
                                    options.FolderLevels),

                            FileOrderBy =
                                options.FileOrderBy,

                            FileOrderDirection =
                                options.FileOrderDirection,

                            DestinationRoot =
                                options.DestinationRoot
                        }
                });
        }

        /// <summary>
        /// Exposes the currently viable organization paths through the
        /// generic Expert decision mechanism.
        ///
        /// This remains null until organization investigation has produced
        /// a report and viable paths have been derived from that report.
        /// </summary>
        public override ExpertDecisionRequest? DecisionRequest
        {
            get
            {
                //---------------------------------------------------------
                // Organization begins with destination confirmation.
                //
                // The default was derived from the selected source folder
                // in BeginProject(). The user may accept that proposal or
                // choose another folder. Only after that collection-level
                // decision is complete are the organization-path choices
                // exposed.
                //---------------------------------------------------------

                // Repair decisions must be exhausted before Organization
                // becomes the next user-facing decision. Organization may
                // already have a valid proposal, but it must not leap ahead
                // of unresolved ebook repair choices.
                if (_repairInvestigation.UnresolvedRepairPaths.Count > 0)
                    return null;

                if (!_organizationDestinationConfirmed)
                {
                    string destination =
                        _organizationInvestigation.Options.DestinationRoot;

                    if (string.IsNullOrWhiteSpace(destination))
                        return null;

                    return new ExpertDecisionRequest
                    {
                        Question =
                            $"I propose creating the organized library here: {destination}. Would you like to use this destination or choose another?",

                        Options =
                        [
                            new ExpertDecisionOption(
                                "organization-destination-default",
                                "Use this destination"),

                            new ExpertDecisionOption(
                                "organization-destination-browse",
                                "Choose a different destination",
                                ExpertDecisionInputKind.Folder)
                        ]
                    };
                }

                // Once the user has committed the organization policy,
                // there is no longer an outstanding organization decision.
                // The Guide must be allowed to leave the decision sequence;
                // otherwise the same path question is regenerated forever.
                if (_organizationCommitted)
                    return null;

                if (_organizationPathOptions.Count == 0)
                    return null;

                return new ExpertDecisionRequest
                {
                    Question =
                        "How would you like Scout to organize this ebook collection?",
                    Options =
                        _organizationPathOptions
                            .Select(path =>
                                new ExpertDecisionOption(
                                    path.Id,
                                    path.Label))
                            .ToArray()
                };
            }
        }

        /// <summary>
        /// Applies a collection-level organization path selected by the user.
        ///
        /// The selected path becomes OrganizationOptions configuration.
        /// No filesystem work occurs here.
        ///
        /// DestinationRoot is deliberately preserved from the existing
        /// organization configuration. Selecting a path does not silently
        /// replace the user's destination.
        /// </summary>
        public override void ApplyDecisionChoice(string optionId)
        {
            ApplyDecisionChoice(optionId, null);
        }

        /// <summary>
        /// Applies either the collection-level destination decision or the
        /// subsequent organization-path decision.
        ///
        /// A folder value is transported through the generic conversation
        /// boundary only when the user chooses a destination other than the
        /// proposed _Organized sibling folder. Ebook Expert owns the meaning
        /// of that value and stores it as OrganizationOptions.DestinationRoot.
        /// </summary>
        public override void ApplyDecisionChoice(
            string optionId,
            string? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(optionId);

            //---------------------------------------------------------
            // Stage 1: destination confirmation / override
            //---------------------------------------------------------

            if (!_organizationDestinationConfirmed)
            {
                switch (optionId)
                {
                    case "organization-destination-default":
                        if (string.IsNullOrWhiteSpace(
                                _organizationInvestigation.Options.DestinationRoot))
                        {
                            throw new InvalidOperationException(
                                "The Ebook organization destination has not been proposed.");
                        }

                        SaveOrganizationDecisionCheckpoint();
                        _organizationDestinationConfirmed = true;
                        return;

                    case "organization-destination-browse":
                        ArgumentException.ThrowIfNullOrWhiteSpace(value);

                        SaveOrganizationDecisionCheckpoint();

                        OrganizationOptions currentDestinationOptions =
                            _organizationInvestigation.Options;

                        _organizationInvestigation.ConfigureOptions(
                            new OrganizationOptions
                            {
                                FolderLevels =
                                    new List<OrganizationDimension>(
                                        currentDestinationOptions.FolderLevels),

                                FileOrderBy =
                                    currentDestinationOptions.FileOrderBy,

                                FileOrderDirection =
                                    currentDestinationOptions.FileOrderDirection,

                                DestinationRoot =
                                    Path.GetFullPath(value)
                            });

                        _organizationDestinationConfirmed = true;
                        return;

                    default:
                        throw new ArgumentException(
                            $"Unknown ebook organization destination option '{optionId}'.",
                            nameof(optionId));
                }
            }

            //---------------------------------------------------------
            // Stage 2: organization path selection
            //---------------------------------------------------------

            OrganizationPathOption? selectedPath =
                _organizationPathOptions.FirstOrDefault(
                    path =>
                        string.Equals(
                            path.Id,
                            optionId,
                            StringComparison.OrdinalIgnoreCase));

            if (selectedPath == null)
            {
                throw new ArgumentException(
                    $"Unknown ebook organization option '{optionId}'.",
                    nameof(optionId));
            }

            OrganizationOptions currentOptions =
                _organizationInvestigation.Options;

            OrganizationOptions options =
                new()
                {
                    FolderLevels =
                        new List<OrganizationDimension>(
                            selectedPath.FolderLevels),

                    FileOrderBy =
                        selectedPath.FileOrderBy,

                    FileOrderDirection =
                        selectedPath.FileOrderDirection,

                    // The destination was confirmed in the previous
                    // collection-level decision and must survive unchanged.
                    DestinationRoot =
                        currentOptions.DestinationRoot
                };

            SaveOrganizationDecisionCheckpoint();

            _organizationCommitted = true;

            _organizationInvestigation.ConfigureOptions(options);

            ExecuteOrganizationAll(_activeProgress);
        }

        private IReadOnlyList<FileContext> _ebookFiles =
            Array.Empty<FileContext>();

        /// <summary>
        /// The selected source folder for the current Ebook expedition.
        ///
        /// This is retained because the source folder establishes the
        /// default Organization destination. It is never replaced by a
        /// temporary Repair workspace path.
        /// </summary>
        private string _sourceFolderPath = string.Empty;

        //---------------------------------------------------------
        // Investigations
        //---------------------------------------------------------

        private readonly E_MetadataInvestigation _metadataInvestigation = new();

        private readonly E_ContentsInvestigation _contentsInvestigation = new();

        private readonly E_OrganizationInvestigation _organizationInvestigation = new();

        private readonly E_DuplicateInvestigation _duplicateInvestigation = new();

        private readonly E_QualityInvestigation _qualityInvestigation = new();

        private readonly E_RepairInvestigation _repairInvestigation = new();

        private readonly E_EnrichmentInvestigation _enrichmentInvestigation = new();

        private readonly E_CoverInvestigation _coverInvestigation = new();

        //---------------------------------------------------------
        // Domain Action Dispatcher
        //---------------------------------------------------------
        //
        // The dispatcher translates generic Conversation Framework
        // action requests into Ebook Expert domain operations.
        //
        // The dispatcher does not perform the investigation itself.
        // It uses the existing investigations and domain services owned
        // by this Expert.
        //
        //---------------------------------------------------------

        private readonly E_ActionDispatcher _actionDispatcher = new();

        // EbookExpert-owned state for a repair opportunity awaiting user-supplied
        // evidence. The generic Conversation Framework remains domain-neutral.
        // The field currently requested through the free-form repair
        // conversation. The stable original path remains the branch key.
        private readonly Dictionary<string, string> _awaitingRepairInformationField =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly IReadOnlyList<ObservationSpecialist> _specialists =
            Array.Empty<ObservationSpecialist>();

        public override IReadOnlyList<ObservationSpecialist> Specialists =>
            _specialists;

        /// <summary>
        /// The organization report produced during the current ebook expedition.
        /// This is working expedition data and remains available until the
        /// expedition lifecycle releases it.
        /// </summary>
        public OrganizationReport? OrganizationReport =>
            _organizationInvestigation.Report;

        //---------------------------------------------------------

        public override string Name =>
            "eBook Library";

        /// <summary>
        /// Ebook observation exposes eight meaningful pipeline stages so
        /// Scout can report domain progress without pretending that one
        /// collection-wide operation is one unit of work.
        /// </summary>
        public override int ProgressStageCount => 8;

        public override string Summary =>
            "I noticed what appears to be a collection of ebooks.";

        public override string WhyItMatters =>
            "Keeping ebooks organized by author, series, or subject makes your library easier to browse and enjoy.";

        /// <summary>
        /// Initializes Ebook Expert project-specific repair state and establishes
        /// the Ebook collection that this Expert will investigate.
        /// </summary>
        public override void BeginProject(
            string sourceFolderPath,
            IReadOnlyList<FileContext> files)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceFolderPath);
            ArgumentNullException.ThrowIfNull(files);

            //---------------------------------------------------------
            // Retain the selected source folder.
            //
            // Organization's default destination is derived from this
            // collection-level source location. We deliberately do not
            // derive it from an individual FileContext.CurrentFullPath
            // because a repaired ebook may later point into the temporary
            // Repair workspace.
            //---------------------------------------------------------

            string normalizedSourceFolder =
                Path.GetFullPath(sourceFolderPath);

            bool newEbookExpedition =
                !string.Equals(
                    _sourceFolderPath,
                    normalizedSourceFolder,
                    StringComparison.OrdinalIgnoreCase);

            if (newEbookExpedition)
            {
                _organizationDecisionHistory.Clear();
                _organizationDestinationConfirmed = false;
                _organizationPathOptions =
                    Array.Empty<OrganizationPathOption>();
                _awaitingRepairInformationField.Clear();
            }

            _sourceFolderPath =
                normalizedSourceFolder;

            //---------------------------------------------------------
            // Establish the Organization default destination.
            //
            // Example:
            //
            //     E:\Books\Ebook
            //
            // becomes:
            //
            //     E:\Books\Ebook_Organized
            //
            // This is the proposed collection destination. The user must
            // still be given the opportunity to accept it or override it
            // through the generic conversation path before organization
            // is considered confirmed.
            //---------------------------------------------------------

            OrganizationOptions currentOrganizationOptions =
                _organizationInvestigation.Options;

            if (newEbookExpedition ||
                string.IsNullOrWhiteSpace(
                    currentOrganizationOptions.DestinationRoot))
            {
                string sourceFolderName =
                    new DirectoryInfo(_sourceFolderPath).Name;

                string parentFolder =
                    Directory.GetParent(_sourceFolderPath)?.FullName
                    ?? _sourceFolderPath;

                string defaultDestination =
                    Path.Combine(
                        parentFolder,
                        sourceFolderName + "_Organized");

                _organizationInvestigation.ConfigureOptions(
                    new OrganizationOptions
                    {
                        FolderLevels =
                            new List<OrganizationDimension>(
                                currentOrganizationOptions.FolderLevels),

                        FileOrderBy =
                            currentOrganizationOptions.FileOrderBy,

                        FileOrderDirection =
                            currentOrganizationOptions.FileOrderDirection,

                        DestinationRoot =
                            defaultDestination
                    });
            }

            //---------------------------------------------------------
            // Ebook discovery
            //---------------------------------------------------------
            //
            // The generic Scout scan may contain many kinds of files.
            // Ebook Expert now establishes its own domain collection.
            //
            // SearchNestedFolders = true:
            //     EPUBs from the selected folder and all nested folders
            //     are included.
            //
            // SearchNestedFolders = false:
            //     only EPUBs directly inside the selected source folder
            //     are included.
            //
            //---------------------------------------------------------

            List<FileContext> ebookFiles = new();

            foreach (FileContext file in files)
            {
                if (file == null)
                    continue;

                if (!string.Equals(
                        file.Extension,
                        ".epub",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!SearchNestedFolders &&
                    !string.IsNullOrWhiteSpace(file.RelativeFolder))
                {
                    continue;
                }

                ebookFiles.Add(file);
            }

            _ebookFiles = ebookFiles;

            //---------------------------------------------------------
            // Repair Expedition
            //---------------------------------------------------------

            _repairInvestigation.BeginExpedition(
                sourceFolderPath,
                _ebookFiles);
        }

        /// <summary>
        /// Allows the Ebook Expert to determine whether the current
        /// repair expedition item is complete after re-observation.
        ///
        /// Completion is deliberately evaluated by the Ebook Expert
        /// because only the domain expert knows what "complete" means
        /// for an ebook repair expedition.
        /// </summary>
        public override bool CompleteCurrentIfComplete()
        {
            return _repairInvestigation.CompleteCurrentIfComplete();
        }

        /// <summary>
        /// =========================================================================
        /// Generation 2 Entry Point
        /// =========================================================================
        /// </summary>
        public override List<ExpertFinding> Investigate(
            IReadOnlyList<FileContext> files)
        {
            return Investigate(
                files,
                null,
                null);
        }

        public override List<ExpertFinding> Investigate(
            IReadOnlyList<FileContext> files,
            string? originalFullPath,
            IProgress<ExecutionProgress>? progress)

        // Begin Investigate()
        {
            _reobservationTargetOriginalFullPath = originalFullPath;
            _activeProgress = progress;

            List<ExpertFinding> findings = new();

            ReportProgress(
                progress,
                0,
                "Metadata",
                "Reading ebook metadata...");

            //---------------------------------------------------------
            // Acquire metadata once.
            //---------------------------------------------------------

            MetadataReport metadataReport =
                _metadataInvestigation.Investigate(
                    _ebookFiles,
                    _sourceFolderPath);

            //---------------------------------------------------------
            // Metadata ExpertFindings
            //---------------------------------------------------------
            //
            // Metadata research is shared with downstream Investigations,
            // but the Metadata Consultant also produces findings that belong
            // in the Ebook Expert's overall understanding.
            //
            //---------------------------------------------------------

            findings.AddRange(
                _metadataInvestigation.Findings);

            ReportProgress(
                progress,
                1,
                "Metadata",
                "Metadata complete.");

            //---------------------------------------------------------
            // Completed Generation 2 Investigations
            //---------------------------------------------------------

            findings.AddRange(
                _contentsInvestigation.Investigate(
                    metadataReport));

            ReportProgress(
                progress,
                2,
                "Contents",
                "Table of contents analysis complete.");

            findings.AddRange(
                _organizationInvestigation.Investigate(
                    metadataReport));

            //---------------------------------------------------------
            // Derive the organization choices from the fresh report.
            //
            // This is deliberately after investigation and before any
            // user decision is exposed.
            //---------------------------------------------------------

            OrganizationReport? organizationReport =
                _organizationInvestigation.Report;

            if (organizationReport != null)
            {
                _organizationPathOptions =
                    _organizationPathBuilder.Build(
                        organizationReport);
            }
            else
            {
                _organizationPathOptions =
                    Array.Empty<OrganizationPathOption>();
            }

            ReportProgress(
                progress,
                3,
                "Organization",
                "Organization analysis complete.");

            findings.AddRange(
                _duplicateInvestigation.Investigate(
                    _ebookFiles));

            ReportProgress(
                progress,
                4,
                "Duplicates",
                "Duplicate analysis complete.");

            findings.AddRange(
                _qualityInvestigation.Investigate(
                    metadataReport));

            ReportProgress(
                progress,
                5,
                "Quality",
                "Quality analysis complete.");

            List<ExpertFinding> repairFindings;

            if (string.IsNullOrWhiteSpace(_reobservationTargetOriginalFullPath))
            {
                repairFindings =
                    _repairInvestigation.Investigate(
                        metadataReport);
            }
            else
            {
                repairFindings =
                    _repairInvestigation.InvestigateBranch(
                        metadataReport,
                        _reobservationTargetOriginalFullPath);
            }

            //---------------------------------------------------------
            // Automatic repair loop
            //---------------------------------------------------------
            //
            // Automatic repair is available only when the user has
            // explicitly delegated authority. A successful repair changes
            // the working EPUB, so re-observe before another pass.
            //
            // Initial investigation may use collection-wide opportunities.
            // A targeted re-observation stays inside its branch so that
            // repairing one EPUB never silently repairs another EPUB.
            //---------------------------------------------------------

            int automaticRepairPass = 0;
            int maximumAutomaticRepairPasses =
                Math.Max(
                    1,
                    _ebookFiles.Count * 2);

            while (automaticRepairPass < maximumAutomaticRepairPasses)
            {
                IReadOnlyList<RepairOpportunity> automaticOpportunities =
                    string.IsNullOrWhiteSpace(
                        _reobservationTargetOriginalFullPath)
                        ? _repairInvestigation.RepairOpportunities
                        : _repairInvestigation.GetRepairOpportunitiesFor(
                            _reobservationTargetOriginalFullPath);

                bool repairApplied =
                    _actionDispatcher.ApplySafeAutomaticRepairs(
                        automaticOpportunities,
                        _repairInvestigation.RepairAuthorization
                            .AutomaticallyHandleQualifyingRepairs);

                if (!repairApplied)
                    break;

                automaticRepairPass++;

                metadataReport =
                    _metadataInvestigation.Investigate(
                        _ebookFiles,
                        _sourceFolderPath);

                if (string.IsNullOrWhiteSpace(
                        _reobservationTargetOriginalFullPath))
                {
                    repairFindings =
                        _repairInvestigation.Investigate(
                            metadataReport);
                }
                else
                {
                    repairFindings =
                        _repairInvestigation.InvestigateBranch(
                            metadataReport,
                            _reobservationTargetOriginalFullPath);
                }
            }

            // Only the final repair pass becomes user-facing. Earlier
            // findings may have been resolved automatically.
            findings.AddRange(repairFindings);

            // Refresh organization metadata after automatic repair so an
            // identity/ISBN change cannot leave the organization plan stale.
            _organizationInvestigation.Investigate(
                metadataReport);

            OrganizationReport? refreshedOrganizationReport =
                _organizationInvestigation.Report;

            if (refreshedOrganizationReport != null)
            {
                _organizationPathOptions =
                    _organizationPathBuilder.Build(
                        refreshedOrganizationReport);
            }

            bool repairWaiting =
                _repairInvestigation.UnresolvedRepairPaths.Count > 0;

            ReportProgress(
                progress,
                6,
                "Repair",
                repairWaiting
                    ? "Repair is waiting for a decision."
                    : "Repair analysis complete.");

            _reobservationTargetOriginalFullPath = null;

            //---------------------------------------------------------
            // Repair → Organization handoff
            //---------------------------------------------------------
            //
            // Organization investigates the collection before Repair,
            // so it does not wait for Repair to finish. Once the Repair
            // investigation has produced any current handoffs, pass
            // those semantic results into the existing organization
            // context. No filesystem work is performed here.
            //
            //---------------------------------------------------------

            _organizationInvestigation.AcceptRepairHandoffs(
                _repairInvestigation.RepairHandoffs,
                _repairInvestigation.UnresolvedRepairPaths);

            findings.AddRange(
                _coverInvestigation.Investigate(
                    metadataReport));

            ReportProgress(
                progress,
                7,
                "Cover",
                "Cover analysis complete.");

            //---------------------------------------------------------
            // Enrichment Investigation
            //---------------------------------------------------------

            findings.AddRange(
                _enrichmentInvestigation.Investigate(
                    metadataReport));

            ReportProgress(
                progress,
                8,
                "Enrichment",
                "Ebook investigation complete.");

            //---------------------------------------------------------
            // Persistent collection-level organization commitment
            //---------------------------------------------------------
            //
            // Once the user has selected an organization policy, every
            // subsequent observation gets an opportunity to organize
            // books that have become ready through repair or enrichment.
            //
            // The organization investigation rebuilds its current
            // Report/Context/Plan/Job from the fresh metadata, while its
            // successful-execution ledger survives those rebuilds.
            //---------------------------------------------------------

            if (_organizationCommitted)
            {
                ExecuteOrganizationAll(_activeProgress);
            }

            return findings;

        } // End Investigate()

        /// <summary>
        /// Performs the normal Ebook observation pass while carrying the
        /// stable identity of the EPUB branch that triggered re-observation.
        /// The ordinary collection investigations remain unchanged; only the
        /// Repair Investigation switches to the targeted branch path.
        /// </summary>
        public override List<ExpertFinding> Investigate(
            IReadOnlyList<FileContext> files,
            string? originalFullPath)
        {
            return Investigate(
                files,
                originalFullPath,
                null);
        }

        /// <summary>
        /// Translates the Expert's findings into conversation-ready
        /// recommendations.
        /// </summary>
        private IReadOnlyList<ExecutionProgressItem> BuildCollectionProgressItems()
        {
            if (_ebookFiles.Count == 0)
                return Array.Empty<ExecutionProgressItem>();

            IReadOnlyList<string> unresolvedPaths =
                _repairInvestigation.UnresolvedRepairPaths;

            return _ebookFiles
                .Select(file =>
                {
                    string originalPath = file.OriginalFullPath;

                    if (_organizationInvestigation.IsOrganized(originalPath))
                    {
                        return new ExecutionProgressItem
                        {
                            Key = originalPath,
                            DisplayName = file.CurrentName,
                            State = "Organized",
                            Status = "Organized.",
                            Completed = 1,
                            Total = 1
                        };
                    }

                    E_RepairHandoff? terminalHandoff =
                        _repairInvestigation.RepairHandoffs.FirstOrDefault(
                            handoff =>
                                string.Equals(
                                    handoff.OriginalPath,
                                    originalPath,
                                    StringComparison.OrdinalIgnoreCase) &&
                                (handoff.Status == E_RepairHandoffStatus.AcceptedAsIs ||
                                 handoff.Status == E_RepairHandoffStatus.Omitted));

                    if (terminalHandoff != null)
                    {
                        bool accepted =
                            terminalHandoff.Status ==
                            E_RepairHandoffStatus.AcceptedAsIs;

                        return new ExecutionProgressItem
                        {
                            Key = originalPath,
                            DisplayName = file.CurrentName,
                            State = accepted ? "Accepted" : "Omitted",
                            Status = accepted
                                ? "Accepted as-is. Ready for Organization."
                                : "Omitted from Organization.",
                            Completed = 1,
                            Total = 1,
                            Actions = Array.Empty<ExecutionProgressAction>()
                        };
                    }

                    E_RepairHandoff? repairHandoff =
                        _repairInvestigation.RepairHandoffs.FirstOrDefault(
                            handoff =>
                                string.Equals(
                                    handoff.OriginalPath,
                                    originalPath,
                                    StringComparison.OrdinalIgnoreCase));

                    if (repairHandoff?.Status == E_RepairHandoffStatus.RepairCompleted)
                    {
                        return new ExecutionProgressItem
                        {
                            Key = originalPath,
                            DisplayName = file.CurrentName,
                            State = "Ready",
                            Status = "Repair complete; ready for Organization.",
                            Completed = 1,
                            Total = 1,
                            Actions = Array.Empty<ExecutionProgressAction>()
                        };
                    }

                    bool waiting =
                        unresolvedPaths.Any(
                            path => string.Equals(
                                path,
                                originalPath,
                                StringComparison.OrdinalIgnoreCase));

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

                    return new ExecutionProgressItem
                    {
                        Key = originalPath,
                        DisplayName = file.CurrentName,
                        State = "Unorganized",
                        Status = waiting
                            ? "Waiting for repair or decision."
                            : "Not yet organized.",
                        Completed = 0,
                        Total = 1,
                        Actions = actions
                    };
                })
                .OrderBy(item =>
                    item.NeedsUserAttention ? 0 :
                    string.Equals(item.State, "Processing", StringComparison.OrdinalIgnoreCase) ? 1 :
                    string.Equals(item.State, "Ready", StringComparison.OrdinalIgnoreCase) ? 2 :
                    item.IsCompleted ? 4 :
                    3)
                .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void ReportProgress(
            IProgress<ExecutionProgress>? progress,
            int stageCompleted,
            string stage,
            string status)
        {
            if (progress == null)
                return;

            int collectionTotal =
                _repairInvestigation.ExpeditionTotal;

            int collectionCompleted =
                _repairInvestigation.ExpeditionCompleted;

            int collectionProcessing =
                _repairInvestigation.ExpeditionProcessing;

            int collectionPending =
                _repairInvestigation.ExpeditionPending;

            int collectionWaiting =
                _repairInvestigation.UnresolvedRepairPaths.Count;

            int collectionActive =
                Math.Max(
                    0,
                    collectionProcessing - collectionWaiting);

            // Before Organization has been committed, the collection
            // progress must come from the repair-aware view. Organization's
            // progress list can be a valid earlier snapshot, but it does not
            // carry the current repair decision buttons. Reusing that stale
            // snapshot makes unresolved books appear to have no next action.
            IReadOnlyList<ExecutionProgressItem> items =
                _organizationCommitted &&
                _organizationInvestigation.ProgressItems.Count > 0
                    ? _organizationInvestigation.ProgressItems
                    : BuildCollectionProgressItems();

            progress.Report(
                new ExecutionProgress
                {
                    Completed = stageCompleted,
                    Total = ProgressStageCount,
                    CurrentFile =
                        _repairInvestigation.CurrentFile?.CurrentName ?? "",
                    Status = status,
                    Stage = stage,
                    StageCompleted = stageCompleted,
                    StageTotal = ProgressStageCount,
                    CollectionTotal = collectionTotal,
                    CollectionCompleted = collectionCompleted,
                    CollectionProcessing = collectionActive,
                    CollectionWaiting = collectionWaiting,
                    CollectionPending = collectionPending,
                    Items = items
                });
        }

        public override List<CV_Recommendation> BuildRecommendations(
            IReadOnlyList<ExpertFinding> findings)
        {
            E_RecommendationTranslator translator = new();

            List<CV_Recommendation> recommendations = new();

            foreach (ExpertFinding finding in findings)
            {
                recommendations.Add(
                    translator.Translate(finding));
            }

            return recommendations;
        }

        /// <summary>
        /// Executes one already-planned organization entry through the
        /// Ebook Expert's organization investigation.
        ///
        /// The Ebook Expert remains the domain boundary. Generic Scout
        /// infrastructure does not need to understand organization details.
        ///
        /// This method executes only the supplied planned entry. It does not
        /// create a queue, manage concurrency, or release the working copy.
        /// </summary>
        internal OrganizationCopyResult ExecuteOrganizationEntry(
            OrganizationPlanEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            return _organizationInvestigation.ExecuteOne(entry);
        }

        /// <summary>
        /// Executes all currently planned organization entries that have not
        /// already completed successfully during this Ebook expedition.
        ///
        /// The collection-level organization primitive remains inside the
        /// Ebook Expert. Generic Scout infrastructure does not need to know
        /// how an ebook collection is organized.
        ///
        /// This does not create concurrency, manage a background queue, or
        /// release working representations.
        ///
        /// The long-lived Ebook Expert can be invoked again after additional
        /// repair or user-decision work has completed. The organization
        /// investigation retains successful execution history and therefore
        /// does not repeat entries that were already organized successfully
        /// during the expedition.
        /// </summary>
        internal IReadOnlyList<OrganizationCopyResult> ExecuteOrganizationAll(
            IProgress<ExecutionProgress>? progress = null)
        {
            return _organizationInvestigation.ExecuteAll(progress);
        }

        /// <summary>
        /// Completes the Ebook branch identified by its stable original path
        /// after re-observation.
        ///
        /// This is the domain-specific handoff from the generic Observation
        /// Framework into the branch-aware Repair Investigation.
        /// </summary>
        public override bool CompleteCurrentIfComplete(
            string? originalFullPath)
        {
            return _repairInvestigation.CompleteCurrentIfComplete(
                originalFullPath);
        }

        /// <summary>
        /// Rebuilds the Ebook Expert's organization context after a terminal
        /// repair decision changes Organization eligibility.
        /// </summary>
        private static string? GetNextUserInputField(
            RepairOpportunity opportunity)
        {
            // Description is deliberately first because it is a direct
            // narrative value Scout can safely accept from the user. The
            // remaining text metadata fields use the same conversation path.
            if (opportunity.MissingDescription)
                return "Description";

            if (opportunity.MissingTitle)
                return "Title";

            if (opportunity.MissingAuthor)
                return "Author";

            if (opportunity.MissingPublisher)
                return "Publisher";

            if (opportunity.MissingLanguage)
                return "Language";

            if (opportunity.IdentityEvaluation?.RepairRequired == true)
            {
                if (opportunity.IdentityEvaluation.SeriesEvaluation?.State ==
                    SeriesEvidenceState.Resolved &&
                    !string.IsNullOrWhiteSpace(
                        opportunity.IdentityEvaluation.Candidate?.Series) &&
                    !string.Equals(
                        opportunity.Record.Metadata.Series,
                        opportunity.IdentityEvaluation.Candidate.Series,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return "Series";
                }

                if (!string.IsNullOrWhiteSpace(
                        opportunity.IdentityEvaluation.Candidate?.SeriesNumber) &&
                    !string.Equals(
                        opportunity.Record.Metadata.SeriesNumber,
                        opportunity.IdentityEvaluation.Candidate.SeriesNumber,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return "SeriesNumber";
                }
            }

            return opportunity.MissingIsbn
                ? "ISBN"
                : null;
        }

        private static string NormalizeUserProvidedFieldValue(
            string field,
            string value)
        {
            string normalized = value.Trim();
            string prefix = field.Trim() + ":";

            if (normalized.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[prefix.Length..].Trim();
            }

            return normalized;
        }

        private static CV_ActionResult BuildUserRepairInformationPrompt(
            CV_ActionRequest request,
            string field,
            string? leadingMessage)
        {
            string prompt =
                field.Equals("Description", StringComparison.OrdinalIgnoreCase)
                    ? "Scout found that Description is missing. If you want Scout to use a description, type it in the chat box below and press Send."
                    : $"Scout found that {field} is missing. If you have a {field} you want Scout to use, type it in the chat box below and press Send.";

            CV_ActionResult result = new()
            {
                ActionId = request.ActionId,
                Success = true,
                Message = string.IsNullOrWhiteSpace(leadingMessage)
                    ? prompt
                    : leadingMessage + Environment.NewLine + Environment.NewLine + prompt
            };

            result.Options.Add(
                new CV_ActionOption
                {
                    Id = "AddRepairInformation",
                    ActionId = "AddRepairInformation",
                    ContextId = request.ContextId,
                    Label = $"Provide {field}",
                    AcceptsUserInput = true,
                    Source = "Ebook Expert"
                });

            return result;
        }

        private static string BuildRepairInformationSummary(
            RepairOpportunity opportunity)
        {
            List<string> missing = new();
            List<string> conflicting = new();

            if (opportunity.MissingTitle)
                missing.Add("Title");

            if (opportunity.MissingAuthor)
                missing.Add("Author");

            if (opportunity.MissingIsbn)
                missing.Add("ISBN");

            if (opportunity.MissingPublisher)
                missing.Add("Publisher");

            if (opportunity.MissingLanguage)
                missing.Add("Language");

            if (opportunity.MissingDescription)
                missing.Add("Description");

            if (opportunity.MissingCover)
                missing.Add("Cover");

            if (opportunity.Record.Reconciliation.Title.State ==
                MetadataFieldReconciliationState.Conflicting)
                conflicting.Add("Title");

            if (opportunity.Record.Reconciliation.Author.State ==
                MetadataFieldReconciliationState.Conflicting)
                conflicting.Add("Author");

            if (opportunity.Record.Reconciliation.Series.State ==
                MetadataFieldReconciliationState.Conflicting)
                conflicting.Add("Series");

            if (opportunity.Record.Reconciliation.SeriesNumber.State ==
                MetadataFieldReconciliationState.Conflicting)
                conflicting.Add("Series Number");

            if (opportunity.IdentityEvaluation?.RepairRequired == true &&
                !string.IsNullOrWhiteSpace(opportunity.IdentityEvaluation.Reason))
            {
                conflicting.Add(
                    "Identity / Series information needs reconciliation");
            }

            List<string> lines = new();

            if (missing.Count > 0)
            {
                lines.Add(
                    "Scout found these missing metadata fields: " +
                    string.Join(", ", missing) + ".");
            }

            if (conflicting.Count > 0)
            {
                lines.Add(
                    "Scout also found information that needs reconciliation: " +
                    string.Join(", ", conflicting) + ".");
            }

            if (lines.Count == 0)
            {
                lines.Add(
                    "Scout found a metadata repair opportunity, but no simple missing field explains it. " +
                    "The evidence needs further evaluation.");
            }

            return string.Join(Environment.NewLine, lines);
        }

        private void RefreshOrganizationAfterRepairDecision()
        {
            _organizationInvestigation.AcceptRepairHandoffs(
                _repairInvestigation.RepairHandoffs,
                _repairInvestigation.UnresolvedRepairPaths);

            if (_organizationCommitted)
                ExecuteOrganizationAll(_activeProgress);
        }

        /// <summary>
        /// Executes an Ebook Expert action requested through the
        /// Conversation Framework.
        ///
        /// The action is routed through the Ebook Expert's Action Dispatcher.
        ///
        /// The Repair Investigation is intentionally passed through here
        /// rather than recreated. This preserves the RepairOpportunity
        /// objects discovered during the most recent investigation.
        ///
        /// Current supported actions:
        ///
        ///     AuthorizeAutomaticAction
        ///     ResearchMissingIsbn
        ///
        /// Future Ebook actions can use this same gateway:
        ///
        ///     ResearchMissingCover
        ///     ResearchMissingSummary
        ///     RepairMissingMetadata
        /// </summary>
        public override CV_ActionResult ExecuteAction(
            CV_ActionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            //---------------------------------------------------------
            // User-level authorization
            //---------------------------------------------------------
            //
            // The Conversation Framework only recognizes and transports
            // the user's delegation.
            //
            // Ebook Expert determines what that delegation means within
            // the ebook domain.
            //
            //---------------------------------------------------------

            if (string.Equals(
                    request.ActionId,
                    "AuthorizeAutomaticAction",
                    StringComparison.OrdinalIgnoreCase))
            {
                _repairInvestigation.AuthorizeAutomaticRepairs();

                //---------------------------------------------------------
                // Authorization may be granted after the initial
                // investigation has already produced repair opportunities.
                // In that case, do not wait for another unrelated action to
                // trigger the automatic-repair pass. Apply the same safe
                // automatic repair capability immediately to the current
                // collection opportunities.
                //
                // A successful physical repair requires re-observation.
                // Authorization by itself does not.
                //---------------------------------------------------------

                bool repairApplied =
                    _actionDispatcher.ApplySafeAutomaticRepairs(
                        _repairInvestigation.RepairOpportunities,
                        _repairInvestigation.RepairAuthorization
                            .AutomaticallyHandleQualifyingRepairs);

                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = true,
                    RequiresReobservation = repairApplied,
                    Message = repairApplied
                        ? "Automatic repairs are now on. I applied the qualifying repairs I could safely determine and will re-check the books."
                        : "I can now handle qualifying ebook repairs that I can safely determine. I'll still ask you when a repair is ambiguous."
                };
            }

            if (string.Equals(
                    request.ActionId,
                    "RevokeAutomaticAction",
                    StringComparison.OrdinalIgnoreCase))
            {
                _repairInvestigation.RevokeAutomaticRepairs();

                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = true,
                    RequiresReobservation = false,
                    Message =
                        "Automatic repairs are now off. I'll ask before applying qualifying repairs."
                };
            }

            //---------------------------------------------------------
            // Unresolved repair user decisions
            //---------------------------------------------------------

            if (string.Equals(request.ActionId, "AcceptAsIs", StringComparison.OrdinalIgnoreCase))
            {
                bool resolved =
                    _repairInvestigation.ResolveUserDecision(
                        request.ContextId,
                        E_RepairHandoffStatus.AcceptedAsIs,
                        "The user explicitly accepted this ebook as-is.");

                _awaitingRepairInformationField.Remove(request.ContextId);

                if (resolved)
                    RefreshOrganizationAfterRepairDecision();

                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = resolved,
                    RequiresReobservation = resolved,
                    Message = resolved
                        ? "This ebook has been accepted as-is. It is no longer blocking repair and is ready for Organization."
                        : "I couldn't match that ebook to an active or pending repair branch."
                };
            }

            if (string.Equals(request.ActionId, "OmitEbook", StringComparison.OrdinalIgnoreCase))
            {
                bool resolved =
                    _repairInvestigation.ResolveUserDecision(
                        request.ContextId,
                        E_RepairHandoffStatus.Omitted,
                        "The user explicitly rejected this ebook for Organization.");

                _awaitingRepairInformationField.Remove(request.ContextId);

                if (resolved)
                    RefreshOrganizationAfterRepairDecision();

                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = resolved,
                    RequiresReobservation = resolved,
                    Message = resolved
                        ? "This ebook has been omitted from Organization and will no longer block the collection."
                        : "I couldn't match that ebook to an active or pending repair branch."
                };
            }

            if (string.Equals(
                    request.ActionId,
                    "ReviewIncompleteMetadata",
                    StringComparison.OrdinalIgnoreCase))
            {
                List<RepairOpportunity> incompleteOpportunities =
                    _repairInvestigation.RepairOpportunities
                        .Where(opportunity => !opportunity.IsComplete)
                        .ToList();

                if (incompleteOpportunities.Count == 0)
                {
                    return new CV_ActionResult
                    {
                        ActionId = request.ActionId,
                        Success = true,
                        Message =
                            "I rechecked the collection and there are no remaining incomplete metadata repair opportunities."
                    };
                }

                CV_ActionResult reviewResult = new()
                {
                    ActionId = request.ActionId,
                    Success = true,
                    Message =
                        incompleteOpportunities.Count == 1
                            ? "I found one ebook that still needs metadata attention. Choose it below and I'll walk you through the missing information."
                            : $"I found {incompleteOpportunities.Count} ebooks that still need metadata attention. Choose the ebook you want to work on."
                };

                foreach (RepairOpportunity opportunity
                    in incompleteOpportunities)
                {
                    string contextId =
                        opportunity.Record.File.OriginalFullPath;

                    if (string.IsNullOrWhiteSpace(contextId))
                        continue;

                    string displayName =
                        !string.IsNullOrWhiteSpace(
                            opportunity.Record.Metadata.Title)
                            ? opportunity.Record.Metadata.Title
                            : Path.GetFileNameWithoutExtension(
                                contextId);

                    reviewResult.Options.Add(
                        new CV_ActionOption
                        {
                            Id = "ReviewRepair:" + contextId,
                            ActionId = "AddRepairInformation",
                            ContextId = contextId,
                            Label = "Review " + displayName,
                            Source = "Ebook Expert"
                        });
                }

                return reviewResult;
            }

            if (string.Equals(request.ActionId, "AddRepairInformation", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(request.ContextId))
                {
                    return new CV_ActionResult
                    {
                        ActionId = request.ActionId,
                        Success = false,
                        Message = "I don't know which ebook needs the additional information."
                    };
                }

                IReadOnlyList<RepairOpportunity> informationOpportunities =
                    _repairInvestigation.GetRepairOpportunitiesFor(
                        request.ContextId);

                RepairOpportunity? informationOpportunity =
                    informationOpportunities.FirstOrDefault();

                if (informationOpportunity == null)
                {
                    return new CV_ActionResult
                    {
                        ActionId = request.ActionId,
                        Success = false,
                        Message = "Scout could not retrieve the current repair details for this ebook."
                    };
                }

                if (!string.IsNullOrWhiteSpace(request.UserInput) &&
                    _awaitingRepairInformationField.TryGetValue(
                        request.ContextId,
                        out string? requestedField))
                {
                    string suppliedValue =
                        NormalizeUserProvidedFieldValue(
                            requestedField,
                            request.UserInput);

                    if (string.IsNullOrWhiteSpace(suppliedValue))
                    {
                        return BuildUserRepairInformationPrompt(
                            request,
                            requestedField,
                            "I need a value for that field before I can add it to the repair plan.");
                    }

                    _repairInvestigation.AddUserFieldEvidence(
                        request.ContextId,
                        requestedField,
                        suppliedValue);

                    CV_ActionResult userInformationResult =
                        _actionDispatcher.ApplyUserProvidedMetadata(
                            request,
                            informationOpportunity,
                            requestedField,
                            suppliedValue,
                            _repairInvestigation.RepairAuthorization
                                .AutomaticallyHandleQualifyingRepairs);

                    if (userInformationResult.Success)
                    {
                        _awaitingRepairInformationField.Remove(
                            request.ContextId);
                    }

                    return userInformationResult;
                }

                string? nextField =
                    GetNextUserInputField(
                        informationOpportunity);

                if (string.IsNullOrWhiteSpace(nextField))
                {
                    return new CV_ActionResult
                    {
                        ActionId = request.ActionId,
                        Success = true,
                        Message =
                            BuildRepairInformationSummary(
                                informationOpportunity) +
                            Environment.NewLine +
                            "Scout does not currently have a text-based repair path for the remaining field(s)."
                    };
                }

                _awaitingRepairInformationField[request.ContextId] =
                    nextField;

                return BuildUserRepairInformationPrompt(
                    request,
                    nextField,
                    null);
            }

            //---------------------------------------------------------
            // Ebook domain action dispatcher
            //---------------------------------------------------------

            CV_ActionResult actionResult =
                _actionDispatcher.Execute(
                    request,
                    _repairInvestigation.RepairOpportunities,
                    _repairInvestigation.RepairAuthorization
                        .AutomaticallyHandleQualifyingRepairs);

            return actionResult;
        }

    } // End EbookExpert

} // End namespace