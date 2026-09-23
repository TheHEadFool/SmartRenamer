using Scout.Observations.Conversation;
using SmartRenamer.Capabilities.TextReplacement;
using SmartRenamer.Models;
using SmartRenamer.Models.Planning;
using SmartRenamer.Observations;
using System;
using System.Collections.Generic;

namespace SmartRenamer.Services
{
    /// <summary>
    /// =========================================================================
    /// ProjectWorkflow
    /// =========================================================================
    ///
    /// Coordinates Scout's complete project workflow.
    ///
    /// The workflow:
    ///
    ///     Investigate
    ///         ↓
    ///     Analyze
    ///         ↓
    ///     Execute recommended capabilities
    ///         ↓
    ///     Observe
    ///         ↓
    ///     Plan
    ///         ↓
    ///     Build Rename Preview
    ///         ↓
    ///     WorkflowResult
    ///
    /// The Observation Framework is deliberately integrated into the existing
    /// workflow rather than replacing the workflow.
    ///
    /// =========================================================================
    /// PROJECT STATUS
    /// =========================================================================
    ///
    /// WHY THIS CLASS EXISTS
    /// -------------------------------------------------------------------------
    /// ProjectWorkflow is the central execution path for Scout's current
    /// project-processing pipeline.
    ///
    /// It coordinates project discovery, analysis, capability execution,
    /// Observation Experts, planning, and rename preview generation.
    ///
    /// =========================================================================
    /// CURRENT MILESTONE
    /// =========================================================================
    ///
    /// Connect the completed EbookExpert to the existing UI.
    ///
    /// =========================================================================
    /// OBSERVATION ARCHITECTURE
    /// =========================================================================
    ///
    /// The ObservationEngine produces ExpertFindings.
    ///
    /// Those SAME findings are used in two places:
    ///
    ///     ExpertFinding
    ///          │
    ///          ├──→ ObservationMapper
    ///          │       ↓
    ///          │   ProjectObservation
    ///          │       ↓
    ///          │   Existing Workspace UI
    ///          │
    ///          └──→ Expert Translator
    ///                  ↓
    ///              CV_Recommendation
    ///                  ↓
    ///              Conversation Framework
    ///
    /// This is intentional.
    ///
    /// The UI and Conversation Framework must describe the same underlying
    /// understanding produced by the domain Expert.
    ///
    /// =========================================================================
    /// MIGRATION NOTE
    /// =========================================================================
    ///
    /// The existing Workspace UI still consumes ProjectObservation.
    ///
    /// The Conversation Framework consumes CV_Recommendation.
    ///
    /// During this migration we deliberately support both contracts.
    ///
    /// The ObservationMapper is the compatibility bridge between the new
    /// ExpertFinding model and the existing ProjectObservation UI model.
    ///
    /// DO NOT remove the legacy Recommendation infrastructure until the
    /// Expert-driven pipeline has been proven in the UI.
    ///
    /// =========================================================================
    /// </summary>
    public class ProjectWorkflow
    {
        //---------------------------------------------------------
        // Core Workflow Components
        //---------------------------------------------------------

        private readonly ProjectInvestigator investigator = new();

        private readonly ProjectAnalyzer analyzer = new();

        private readonly ScoutPlanner planner = new();

        private readonly RenamePreviewBuilder previewBuilder = new();

        private readonly CapabilityFactory capabilityFactory = new();

        //---------------------------------------------------------
        // Observation Framework
        //---------------------------------------------------------

        /// <summary>
        /// Coordinates all domain Observation Experts.
        ///
        /// The ObservationEngine is the boundary between the main workflow
        /// and the self-contained domain Experts.
        /// </summary>
        private readonly ObservationEngine observationEngine = new();


        //---------------------------------------------------------
        // Active Project Files
        //---------------------------------------------------------
        //
        // Retained so a repair action can re-observe the same
        // FileContext collection after the physical file changes.
        //

        private IReadOnlyList<FileContext>? activeFiles;
        private string? activeSourceFolderPath;

        //---------------------------------------------------------
        // Re-observation Results
        //---------------------------------------------------------
        //
        // A domain action may require the active project to be observed
        // again. Preserve the recommendations produced by that pass so
        // the Guide can continue from the new workflow state instead of
        // remaining on the recommendation that initiated the action.
        //

        private IReadOnlyList<CV_Recommendation>
            lastReobservationRecommendations =
                Array.Empty<CV_Recommendation>();

        /// <summary>
        /// Recommendations produced by the most recent action-triggered
        /// re-observation.
        ///
        /// ProjectWorkflow does not interpret these recommendations.
        /// It only preserves the workflow result for the Guide.
        /// </summary>
        public IReadOnlyList<CV_Recommendation>
            LastReobservationRecommendations =>
                lastReobservationRecommendations;

        //---------------------------------------------------------
        // New Workflow
        //---------------------------------------------------------

        /// <summary>
        /// Starts a new workflow by asking the user to choose a folder.
        /// </summary>
        public WorkflowResult? Execute()
        {
            ProjectContext? context =
                investigator.Investigate();

            if (context == null)
                return null;

            return Execute(context);
        }

        //---------------------------------------------------------
        // Discovery Choices
        //---------------------------------------------------------

        /// <summary>
        /// Returns the discovery choices currently offered by the registered
        /// Observation Experts.
        ///
        /// ProjectWorkflow exposes this generic bridge so the Guide can collect
        /// required user choices without reaching directly into the
        /// ObservationEngine.
        /// </summary>
        public IReadOnlyList<ExpertDiscoveryBinding> DiscoveryBindings =>
            observationEngine.DiscoveryBindings;

        /// <summary>
        /// Applies a user-selected discovery choice to the Expert that owns it.
        ///
        /// The workflow transports the choice; the domain Expert owns its
        /// meaning.
        /// </summary>
        public void ApplyDiscoveryChoice(
            string expertName,
            string optionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(expertName);
            ArgumentException.ThrowIfNullOrWhiteSpace(optionId);

            observationEngine.ApplyDiscoveryChoice(
                expertName,
                optionId);
        }
        //---------------------------------------------------------
        // Expert Decisions
        //---------------------------------------------------------

        /// <summary>
        /// Returns the generic decision requests currently supplied by
        /// the registered Observation Experts.
        ///
        /// ProjectWorkflow transports the requests without interpreting
        /// their domain meaning.
        /// </summary>
        public IReadOnlyList<ExpertDecisionRequest> DecisionRequests =>
            observationEngine.DecisionRequests;

        /// <summary>
        /// Returns bindings between generic decision requests and the
        /// Experts that own them.
        /// </summary>
        public IReadOnlyList<ExpertDecisionBinding> DecisionBindings =>
            observationEngine.DecisionBindings;

        /// <summary>
        /// Applies a user-selected decision to the Expert that owns it.
        ///
        /// ProjectWorkflow transports the choice. The domain Expert owns
        /// its meaning.
        /// </summary>
        public void ApplyDecisionChoice(
            string expertName,
            string optionId)
        {
            ApplyDecisionChoice(
                expertName,
                optionId,
                null);
        }

        /// <summary>
        /// Transports a generic decision choice and an optional user-supplied
        /// value through the workflow to the owning Observation Expert.
        ///
        /// ProjectWorkflow does not interpret either value. It remains only
        /// the generic transport boundary between Guide and ObservationEngine.
        /// </summary>
        public void ApplyDecisionChoice(
            string expertName,
            string optionId,
            string? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(expertName);
            ArgumentException.ThrowIfNullOrWhiteSpace(optionId);

            observationEngine.ApplyDecisionChoice(
                expertName,
                optionId,
                value);
        }
        //---------------------------------------------------------
        // Existing Project Workflow
        //---------------------------------------------------------

        /// <summary>
        /// Rebuilds the workflow using an existing project.
        ///
        /// This is used after renaming so the preview can be refreshed.
        ///
        /// The Observation Framework is executed as part of the same workflow
        /// so the Expert findings and recommendations always correspond to
        /// the currently discovered project.
        /// </summary>
        public WorkflowResult Execute(ProjectContext context)
        {

            ArgumentNullException.ThrowIfNull(context);

            FolderSummary folder =
                context.Folder
                ?? throw new InvalidOperationException(
                    "Cannot execute the workflow because no folder is available.");

            activeFiles =
                folder.FileContexts;
            activeSourceFolderPath =
                folder.FolderPath;

            //---------------------------------------------------------
            // A full workflow execution establishes a new observation
            // state. Any action-triggered re-observation results from the
            // previous workflow are no longer current.
            //---------------------------------------------------------

            lastReobservationRecommendations =
                Array.Empty<CV_Recommendation>();

            //---------------------------------------------------------
            // Analyze the project.
            //---------------------------------------------------------

            analyzer.Analyze(context);

            //---------------------------------------------------------
            // Execute the recommended capabilities on every discovered file.
            //---------------------------------------------------------

            foreach (string capabilityName in context.RecommendedCapabilities)
            {
                WorkflowStep? workflowStep =
                    capabilityFactory.Create(capabilityName);

                if (workflowStep == null)
                    continue;

                foreach (FileContext file in context.Folder.FileContexts)
                {
                    workflowStep.Step.Execute(file);
                }
            }

            //---------------------------------------------------------
            // Observation Framework
            //---------------------------------------------------------
            //
            // The completed domain Experts observe the discovered
            // collection and produce conversation-ready recommendations.
            //
            // ObservationEngine also preserves the exact ExpertFindings
            // produced during this observation pass.
            //
            //---------------------------------------------------------

            List<CV_Recommendation> observationRecommendations =
                observationEngine.Observe(
                    context.Folder.FileContexts,
                    context.Folder.FolderPath);

            //---------------------------------------------------------
            // Observation Framework → Existing UI
            //---------------------------------------------------------
            //
            // The existing Workspace UI consumes ProjectObservation.
            //
            // The new Expert architecture produces ExpertFinding.
            //
            // ObservationMapper is the deliberate compatibility bridge
            // between those two models.
            //
            // IMPORTANT:
            //
            // We use ObservationEngine.Findings here rather than running
            // the Experts again. This guarantees the UI and Conversation
            // Framework are based on the SAME observation pass.
            //
            // We clear the previous observations because the new Expert
            // findings are now the authoritative observations for this
            // workflow pass.
            //
            // This prevents legacy generic observations such as:
            //
            //     Project Type
            //     Related Files
            //     Audio Collection
            //
            // from appearing alongside the domain-specific Expert
            // observations.
            //
            //---------------------------------------------------------

            List<ProjectObservation> observationUiItems =
                ObservationMapper.Map(
                    observationEngine.Findings);

            context.Observations.Clear();

            context.Observations.AddRange(
                observationUiItems);

            //---------------------------------------------------------
            // Build Scout's plan.
            //---------------------------------------------------------

            ScoutPlan plan =
                planner.Build(context);

            //---------------------------------------------------------
            // Build the rename preview.
            //---------------------------------------------------------

            var preview =
                previewBuilder.Build(
                    context,
                    plan);

            //---------------------------------------------------------
            // Refresh the plan's rename preview.
            //---------------------------------------------------------

            plan.RenamePreview.Clear();

            plan.RenamePreview.AddRange(preview);

            //---------------------------------------------------------
            // Return the complete workflow result.
            //---------------------------------------------------------

            return new WorkflowResult
            {
                Project = context,
                Plan = plan,
                Preview = preview,

                ObservationRecommendations =
                   observationRecommendations
            };
        }

        /// <summary>
        /// Executes a Conversation Framework action through the
        /// ObservationEngine owned by this workflow.
        ///
        /// The same ObservationEngine that performed the investigation
        /// executes the action, preserving the domain Expert state from
        /// that investigation.
        /// </summary>
        public CV_ActionResult ExecuteAction(
            CV_ActionRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            //---------------------------------------------------------
            // Clear the previous action's re-observation result before
            // executing a new action.
            //---------------------------------------------------------

            lastReobservationRecommendations =
                Array.Empty<CV_Recommendation>();

            CV_ActionResult result =
                observationEngine.ExecuteAction(request);

            //---------------------------------------------------------
            // A successful domain action may change the physical
            // state represented by the active FileContext collection.
            //
            // When the domain reports that re-observation is required,
            // run the existing ObservationEngine again against the
            // same FileContext objects.
            //
            // Preserve the resulting recommendations so the Guide can
            // continue from the newly observed workflow state.
            //
            // The workflow remains domain-neutral. It does not inspect
            // the ActionId to determine what happened.
            //---------------------------------------------------------

            if (result.Success &&
                result.RequiresReobservation)
            {
                lastReobservationRecommendations =
                    Reobserve();

                observationEngine.CompleteCurrentIfComplete();
            }

            return result;
        }

        //---------------------------------------------------------
        // Re-observation
        //---------------------------------------------------------

        /// <summary>
        /// Runs the Observation Framework again against the current
        /// FileContext collection.
        ///
        /// This is intentionally smaller than Execute().
        ///
        /// A repair action may change the physical file represented by
        /// a FileContext. The repaired file must therefore be observed
        /// again without rebuilding the entire project workflow.
        ///
        /// The existing ObservationEngine is reused so persistent Expert
        /// state is preserved.
        /// </summary>
        public List<CV_Recommendation> Reobserve()
        {
            if (activeFiles == null ||
                string.IsNullOrWhiteSpace(activeSourceFolderPath))
                throw new InvalidOperationException(
                    "Cannot re-observe because no project is currently active.");

            return observationEngine.Observe(
                activeFiles,
                activeSourceFolderPath);
        }

    }



}