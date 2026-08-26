using System;
using System.Collections.Generic;

namespace Scout.Observations.Conversation
{
    /// <summary>
    /// =========================================================================
    /// CV_Recommendation
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Recommend the next best step."
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Represents one recommendation Scout would like to discuss with the user.
    ///
    /// A recommendation is the conversational representation of an
    /// ExpertFinding. Its Id allows the Conversation Framework, Review All
    /// system, Workspace, and Expert findings to refer to the same discovery.
    ///
    /// A recommendation may also contain an optional next-step action.
    ///
    /// ActionId identifies the capability associated with the recommendation.
    /// ActionText is the human-readable text presented to the user.
    ///
    /// CV_Recommendation does NOT execute the action.
    /// It only describes the recommended next step.
    ///
    /// =========================================================================
    /// Responsibilities
    /// =========================================================================
    ///
    /// • Identify the underlying ExpertFinding.
    /// • Describe the recommendation.
    /// • Explain why it matters.
    /// • Preserve supporting evidence.
    /// • Provide the question Scout can use when discussing it.
    /// • Describe an optional next-step action.
    ///
    /// =========================================================================
    /// This class does NOT
    /// =========================================================================
    ///
    /// • Analyze files.
    /// • Perform research.
    /// • Execute actions.
    /// • Decide which recommendation should be selected.
    /// • Communicate directly with the user.
    ///
    /// Those responsibilities belong to the Experts, Investigations,
    /// Conversation Engine, Selectors, and action/capability layer.
    ///
    /// =========================================================================
    /// MIGRATION NOTE
    /// =========================================================================
    ///
    /// Id was introduced during the transition from the legacy
    /// ProjectObservation path to the Expert-driven Observation Framework.
    ///
    /// The Id should correspond to the identity of the ExpertFinding represented
    /// by this recommendation.
    ///
    /// This identity will remain useful after the legacy observation path is
    /// removed because it provides a stable connection between:
    ///
    ///     ExpertFinding
    ///          ↓
    ///     CV_Recommendation
    ///          ↓
    ///     Conversation
    ///          ↓
    ///     Action / Capability
    ///
    /// =========================================================================
    /// </summary>
    public sealed class CV_Recommendation
    {
        /// <summary>
        /// Identity of the ExpertFinding represented by this recommendation.
        ///
        /// This provides the stable connection between the Expert's discovery
        /// and Scout's conversational representation of that discovery.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Human-readable title for the recommendation.
        /// </summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// Question Scout can ask when discussing this recommendation.
        /// </summary>
        public string Question { get; init; } = string.Empty;

        /// <summary>
        /// Explanation of why this recommendation matters.
        /// </summary>
        public string Reason { get; init; } = string.Empty;

        /// <summary>
        /// Supporting evidence supplied by the Expert.
        ///
        /// The Conversation Framework may use this evidence when explaining
        /// the recommendation to the user.
        /// </summary>
        public List<string> Evidence { get; } = new();

        /// <summary>
        /// Safety information associated with the recommendation.
        /// </summary>
        public string SafetyMessage { get; init; } = string.Empty;

        /// <summary>
        /// Benefits of taking the recommended action.
        /// </summary>
        public List<string> Benefits { get; } = new();

        /// <summary>
        /// Optional estimated time associated with the recommendation.
        ///
        /// This is informational only. It does not control execution.
        /// </summary>
        public string EstimatedTime { get; init; } = string.Empty;

        // =====================================================================
        // Next-Step Action
        // =====================================================================

        /// <summary>
        /// Stable identifier for the next-step capability associated with this
        /// recommendation.
        ///
        /// ActionId identifies what Scout wants the user to explore or do.
        /// It does not execute the operation.
        ///
        /// Examples:
        ///
        ///     "ResearchMissingIsbn"
        ///     "RepairMissingMetadata"
        ///     "ReviewDuplicates"
        ///
        /// The actual execution of the capability belongs outside this class.
        /// </summary>
        public string ActionId { get; init; } = string.Empty;

        /// <summary>
        /// Human-readable text displayed to the user for the next-step action.
        ///
        /// Examples:
        ///
        ///     "Research Missing ISBNs"
        ///     "Repair Metadata"
        ///     "Review Duplicates"
        /// </summary>
        public string ActionText { get; init; } = string.Empty;

        /// <summary>
        /// Indicates whether this recommendation exposes a next-step action.
        /// </summary>
        public bool HasAction =>
            !string.IsNullOrWhiteSpace(ActionId);
    }
}