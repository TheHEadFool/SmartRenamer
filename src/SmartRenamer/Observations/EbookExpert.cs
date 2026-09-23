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
            ArgumentException.ThrowIfNullOrWhiteSpace(optionId);

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

                    DestinationRoot =
                        currentOptions.DestinationRoot
                };

            _organizationInvestigation.ConfigureOptions(options);

            ExecuteOrganizationAll();
        }

        private IReadOnlyList<FileContext> _ebookFiles =
            Array.Empty<FileContext>();

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

            findings.AddRange(
                _repairInvestigation.Investigate(
                    metadataReport));

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

            return findings;

        } // End Investigate()

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