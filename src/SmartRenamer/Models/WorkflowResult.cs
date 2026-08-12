using System.Collections.Generic;
using Scout.Observations.Conversation;
using SmartRenamer.Models.Planning;
using SmartRenamer.Models.Rename;

namespace SmartRenamer.Models
{
    /// <summary>
    /// =========================================================================
    /// WorkflowResult
    /// =========================================================================
    ///
    /// Carries the results of Scout's analysis through the application.
    ///
    /// The WorkflowResult is the hand-off point between the workflow and the
    /// Guide/UI.
    ///
    /// =========================================================================
    /// PROJECT STATUS
    /// =========================================================================
    ///
    /// WHY THIS CLASS EXISTS
    /// -------------------------------------------------------------------------
    /// Provides a single result object containing the information discovered
    /// while Scout processes a project.
    ///
    /// CURRENT MILESTONE
    /// -------------------------------------------------------------------------
    /// Connect the new Observation Framework to the existing UI.
    ///
    /// CURRENT STATUS
    /// -------------------------------------------------------------------------
    /// ✓ Project context carried through workflow
    /// ✓ Scout plan carried through workflow
    /// ✓ Rename preview carried through workflow
    /// ✓ Observation recommendations now have a place to travel
    /// ☐ ProjectWorkflow populates ObservationRecommendations
    /// ☐ Existing Recommendation UI consumes the new recommendations
    /// ☐ Conversation Framework consumes the same recommendations
    ///
    /// CURRENTLY DRIVING
    /// -------------------------------------------------------------------------
    /// The transition from the legacy recommendation pipeline to the
    /// Expert-generated recommendation pipeline.
    ///
    /// DO NOT CHANGE UNTIL
    /// -------------------------------------------------------------------------
    /// The Ebook Expert's recommendations are visible in the UI.
    ///
    /// Do not remove the existing Recommendation pipeline yet. It remains
    /// available while the new Expert-driven pipeline is being proven.
    ///
    /// EXPERT FACTORY
    /// -------------------------------------------------------------------------
    /// INDIRECT
    ///
    /// This is a shared framework model rather than an Expert template.
    /// However, it carries the shared CV_Recommendation contract that every
    /// generated Expert will eventually use.
    ///
    /// NEXT STEP
    /// -------------------------------------------------------------------------
    /// Update ProjectWorkflow.Execute(ProjectContext) so that ObservationEngine
    /// populates ObservationRecommendations.
    ///
    /// =========================================================================
    /// </summary>
    public class WorkflowResult
    {
        //---------------------------------------------------------
        // Project
        //---------------------------------------------------------

        public ProjectContext Project { get; set; } = new();

        //---------------------------------------------------------
        // Scout Plan
        //---------------------------------------------------------

        public ScoutPlan Plan { get; set; } = new();

        //---------------------------------------------------------
        // Rename Preview
        //---------------------------------------------------------

        public List<RenamePreview> Preview { get; set; } = new();

        //---------------------------------------------------------
        // Observation Framework
        //---------------------------------------------------------

        /// <summary>
        /// Recommendations produced by the Observation Framework.
        ///
        /// These originate inside the domain Experts:
        ///
        /// ObservationExpert
        ///     ↓
        /// Investigations
        ///     ↓
        /// ExpertFindings
        ///     ↓
        /// Recommendation Translator
        ///     ↓
        /// CV_Recommendation
        ///
        /// They are deliberately kept separate from the legacy
        /// SmartRenamer.Models.Recommendations.Recommendation collection
        /// while the new pipeline is being connected to the UI.
        /// </summary>
        public List<CV_Recommendation> ObservationRecommendations { get; set; }
            = new();
    }
}