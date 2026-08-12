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
    /// CURRENT MILESTONE
    /// -------------------------------------------------------------------------
    /// Connect the completed EbookExpert to the existing UI.
    ///
    /// CURRENT STATUS
    /// -------------------------------------------------------------------------
    /// ✓ Project investigation
    /// ✓ Project analysis
    /// ✓ Capability execution
    /// ✓ Observation Framework integration
    /// ✓ Scout planning
    /// ✓ Rename preview generation
    /// ✓ Observation recommendations carried by WorkflowResult
    /// ☐ Observation recommendations displayed by the UI
    /// ☐ Conversation uses the same recommendation state
    ///
    /// CURRENTLY DRIVING
    /// -------------------------------------------------------------------------
    /// The transition from the existing workflow to the Expert-driven
    /// observation and recommendation pipeline.
    ///
    /// IMPORTANT
    /// -------------------------------------------------------------------------
    /// The existing workflow is NOT being replaced.
    ///
    /// The Observation Framework is being connected alongside the existing
    /// workflow so we can observe what the completed Experts actually produce
    /// before deciding what legacy code can safely be removed.
    ///
    /// DO NOT CHANGE UNTIL
    /// -------------------------------------------------------------------------
    /// EbookExpert recommendations are visible in the existing UI.
    ///
    /// In particular, do not remove the existing RecommendationBuilder,
    /// Recommendation model, or other legacy recommendation infrastructure
    /// merely because the Observation Framework now produces recommendations.
    ///
    /// EXPERT FACTORY
    /// -------------------------------------------------------------------------
    /// INDIRECT
    ///
    /// This class is shared infrastructure rather than an Expert template.
    /// It provides the execution path through which future generated Experts
    /// will participate in Scout.
    ///
    /// NEXT STEP
    /// -------------------------------------------------------------------------
    /// Connect WorkflowResult.ObservationRecommendations to the existing
    /// Recommendation/UI pipeline.
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
        /// so the Expert recommendations always correspond to the currently
        /// discovered project.
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
            // The completed domain Experts now observe the discovered
            // collection and produce conversation-ready recommendations.
            //
            // This is the integration point between the existing workflow
            // and the new Expert architecture.
            //
            // We intentionally keep these recommendations separate from
            // the existing Recommendation system for now.
            //
            // That allows the UI to show us what the Experts actually
            // produce before we remove or replace legacy infrastructure.
            //---------------------------------------------------------

            List<CV_Recommendation> observationRecommendations =
                observationEngine.Observe(
                    context.Folder.FileContexts);

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

                // New Expert-driven recommendation pipeline.
                ObservationRecommendations =
                    observationRecommendations
            };
        }
    }
}