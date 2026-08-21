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
    /// Each recommendation is associated with the ExpertFinding that produced
    /// it through Id. This allows the Conversation Framework, Review All report,
    /// and Workspace UI to refer to the same underlying finding.
    ///
    /// Future Responsibilities
    /// -------------------------------------------------------------------------
    /// • Describe the recommendation.
    /// • Explain why it is valuable.
    /// • Record confidence.
    /// • Provide supporting evidence.
    /// • Suggest the next question Scout should ask.
    /// • Track its current status.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Decide whether it should be selected.
    /// • Analyze files.
    /// • Communicate directly with the user.
    ///
    /// Those responsibilities belong to the Conversation Planner
    /// and the Experts.
    ///
    /// =========================================================================
    /// MIGRATION NOTE
    /// =========================================================================
    ///
    /// Id is being introduced during the transition from the legacy
    /// ProjectObservation path to the Conversation Framework.
    ///
    /// The Id allows both representations to refer to the same ExpertFinding.
    ///
    /// Once the legacy path is removed, this identity will remain useful for
    /// connecting recommendations to reports, UI actions, and Expert findings.
    ///
    /// =========================================================================
    /// </summary>
    public sealed class CV_Recommendation
    {
        /// <summary>
        /// Identity of the ExpertFinding represented by this recommendation.
        ///
        /// This should match the Id assigned to the corresponding
        /// ProjectObservation.
        /// </summary>
        public Guid Id { get; init; }

        /// <summary>
        /// Human-readable title for the recommendation.
        /// </summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// Question Scout can ask when discussing this recommendation.
        /// </summary>
        /// 

        /// <summary>
        /// Stable identifier for the next-step link associated with this
        /// recommendation.
        ///
        /// This identifies what Scout wants the user to explore or discuss.
        /// It does not execute the operation.
        ///
        /// Future action implementations can use the same identifier when
        /// actual execution is added.
        /// </summary>
        public string ActionId { get; init; } = string.Empty;

        /// <summary>
        /// Text displayed as the clickable next-step link.
        /// </summary>
        public string ActionText { get; init; } = string.Empty;

        /// <summary>
        /// True when this recommendation has a clickable next step.
        /// </summary>
        public bool HasAction =>
            !string.IsNullOrWhiteSpace(ActionId);
        public string Question { get; init; } = string.Empty;

        /// <summary>
        /// Explanation of why this recommendation matters.
        /// </summary>
        public string Reason { get; init; } = string.Empty;

        /// <summary>
        /// Supporting evidence supplied by the Expert.
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
        /// This is retained for future use. It may eventually be used to
        /// represent Scout's progress while working, but it is not required
        /// for the current Review All implementation.
        /// </summary>
        public string EstimatedTime { get; init; } = string.Empty;
    }
}