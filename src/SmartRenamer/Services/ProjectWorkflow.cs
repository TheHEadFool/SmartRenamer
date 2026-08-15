using Scout.Observations.Conversation;
using SmartRenamer.Capabilities.TextReplacement;
using SmartRenamer.Models;
using SmartRenamer.Models.Planning;
using SmartRenamer.Observations;
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
                    context.Folder.FileContexts);

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

                //-----------------------------------------------------
                // New Expert-driven recommendation pipeline.
                //-----------------------------------------------------

                ObservationRecommendations =
                    observationRecommendations
            };
        }
    }
}