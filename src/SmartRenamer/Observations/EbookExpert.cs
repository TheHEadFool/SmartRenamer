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

            ExecuteOrganizationAll();
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

        // Begin Investigate()
        {
            List<ExpertFinding> findings = new();

            //---------------------------------------------------------
            // Acquire metadata once.
            //---------------------------------------------------------

            MetadataReport metadataReport =
                _metadataInvestigation.Investigate(_ebookFiles);

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

            //---------------------------------------------------------
            // Completed Generation 2 Investigations
            //---------------------------------------------------------

            findings.AddRange(
                _contentsInvestigation.Investigate(
                    metadataReport));

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

            findings.AddRange(
                _duplicateInvestigation.Investigate(
                    _ebookFiles));

            findings.AddRange(
                _qualityInvestigation.Investigate(
                    metadataReport));

            if (string.IsNullOrWhiteSpace(_reobservationTargetOriginalFullPath))
            {
                findings.AddRange(
                    _repairInvestigation.Investigate(
                        metadataReport));
            }
            else
            {
                findings.AddRange(
                    _repairInvestigation.InvestigateBranch(
                        metadataReport,
                        _reobservationTargetOriginalFullPath));
            }

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
                _repairInvestigation.RepairHandoffs);

            findings.AddRange(
                _coverInvestigation.Investigate(
                    metadataReport));

            //---------------------------------------------------------
            // Enrichment Investigation
            //---------------------------------------------------------

            findings.AddRange(
                _enrichmentInvestigation.Investigate(
                    metadataReport));

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
                ExecuteOrganizationAll();
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
            _reobservationTargetOriginalFullPath = originalFullPath;

            return Investigate(files);
        }

        /// <summary>
        /// Translates the Expert's findings into conversation-ready
        /// recommendations.
        /// </summary>
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
        internal IReadOnlyList<OrganizationCopyResult> ExecuteOrganizationAll()
        {
            return _organizationInvestigation.ExecuteAll();
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

                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = true,
                    RequiresReobservation = false,
                    Message =
                        "I can now handle qualifying ebook repairs that I can safely determine. I'll still ask you when a repair is ambiguous."
                };
            }

            //---------------------------------------------------------
            // Legacy whole-ebook defer action
            //---------------------------------------------------------
            //
            // This remains temporarily supported while the repair
            // opportunity-level skip behavior is being rebuilt.
            //
            //---------------------------------------------------------

            if (string.Equals(
                    request.ActionId,
                    "SkipCurrentEbook",
                    StringComparison.OrdinalIgnoreCase))
            {
                bool deferred =
                    _repairInvestigation.DeferCurrent();

                return new CV_ActionResult
                {
                    ActionId = request.ActionId,
                    Success = deferred,
                    RequiresReobservation = deferred,
                    Message = deferred
                        ? "I've set this ebook aside and will continue with the next one."
                        : "There is no current ebook to skip."
                };
            }

            //---------------------------------------------------------
            // Ebook domain action dispatcher
            //---------------------------------------------------------

            return _actionDispatcher.Execute(
                request,
                _repairInvestigation.RepairOpportunities,
                _repairInvestigation.RepairAuthorization
                    .AutomaticallyHandleQualifyingRepairs);
        }

    } // End EbookExpert

} // End namespace