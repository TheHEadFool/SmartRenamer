using System;
using System.Collections.Generic;
using SmartRenamer.Models;

namespace Scout.Observations.Conversation
{
    /// <summary>
    /// =========================================================================
    /// CV_ReviewAllItem
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Show the complete picture without losing the path to action."
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Represents one item in Scout's complete Review All report.
    ///
    /// Review All is different from a normal conversation.
    ///
    /// Normal conversation:
    ///
    ///     CV_Recommendation
    ///          ↓
    ///     CV_CurrentTopic
    ///          ↓
    ///     Scout discusses one finding
    ///
    /// Review All:
    ///
    ///     CV_Recommendation
    ///          ↓
    ///     CV_ReviewAllItem
    ///          ↓
    ///     Complete Review All report
    ///
    /// Each Review All item represents the SAME underlying finding that is
    /// available through the Workspace's quick-access observation buttons.
    ///
    /// This allows a user to reach the same finding in two ways:
    ///
    ///     Left-side button
    ///          OR
    ///     Review All report link
    ///
    /// Both paths should ultimately lead to the same ProjectObservation and
    /// therefore the same Scout conversation.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Represent one recommendation in the Review All report.
    /// • Preserve the information supplied by the Conversation Framework.
    /// • Preserve the identity of the underlying finding.
    /// • Provide a link back to the corresponding ProjectObservation.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Analyze files.
    /// • Create ExpertFindings.
    /// • Translate ExpertFindings.
    /// • Decide which recommendation is most important.
    /// • Execute an action.
    /// • Communicate directly with the user.
    ///
    /// Those responsibilities belong to Experts, Translators,
    /// the Conversation Engine, and the Workspace.
    ///
    /// =========================================================================
    /// MIGRATION NOTE
    /// =========================================================================
    ///
    /// This class belongs to the NEW Conversation Framework.
    ///
    /// The existing ProjectObservation system remains temporarily in place
    /// while the new Conversation Framework replaces the legacy presentation
    /// paths.
    ///
    /// The ProjectObservation reference below is intentional during migration.
    /// It allows the new Review All report to connect directly to the existing
    /// Workspace observation buttons.
    ///
    /// DO NOT create a second independent representation of the finding.
    ///
    /// The Id carried by this object must remain the same Id used by the
    /// ExpertFinding, CV_Recommendation, and ProjectObservation representing
    /// the same underlying discovery.
    ///
    /// When the new Conversation Framework completely replaces the legacy
    /// Workspace observation system, this compatibility reference can be
    /// reconsidered.
    ///
    /// =========================================================================
    /// </summary>
    public sealed class CV_ReviewAllItem
    {
        /// <summary>
        /// The recommendation represented by this Review All item.
        ///
        /// This remains the authoritative conversation information.
        /// </summary>
        public CV_Recommendation Recommendation { get; }

        /// <summary>
        /// The existing Workspace observation represented by this report item.
        ///
        /// This provides the migration bridge between the new Conversation
        /// Framework and the existing left-side observation buttons.
        /// </summary>
        public ProjectObservation? Observation { get; }

        /// <summary>
        /// Stable identity of the underlying finding.
        ///
        /// This should match:
        ///
        ///     ExpertFinding.Id
        ///     ProjectObservation.Id
        ///     CV_Recommendation.Id
        ///
        /// The shared identity is what will allow a Review All report link
        /// to select the corresponding observation in the Workspace.
        /// </summary>
        public Guid Id =>
            Recommendation.Id;

        /// <summary>
        /// Creates a Review All item from a Conversation recommendation.
        /// </summary>
        public CV_ReviewAllItem(
            CV_Recommendation recommendation,
            ProjectObservation? observation = null)
        {
            Recommendation =
                recommendation ??
                throw new ArgumentNullException(
                    nameof(recommendation));

            Observation = observation;
        }

        /// <summary>
        /// The title displayed for this finding in the Review All report.
        /// </summary>
        public string Title =>
            !string.IsNullOrWhiteSpace(Recommendation.Title)
                ? Recommendation.Title
                : Recommendation.Reason;

        /// <summary>
        /// The primary explanation shown in the Review All report.
        /// </summary>
        public string Reason =>
            Recommendation.Reason;

        /// <summary>
        /// Supporting evidence for the finding.
        /// </summary>
        public IReadOnlyList<string> Evidence =>
            Recommendation.Evidence;

        /// <summary>
        /// Benefits associated with addressing the finding.
        /// </summary>
        public IReadOnlyList<string> Benefits =>
            Recommendation.Benefits;

        /// <summary>
        /// Safety information associated with the finding.
        /// </summary>
        public string SafetyMessage =>
            Recommendation.SafetyMessage;

        /// <summary>
        /// The question Scout can use when the user chooses to discuss
        /// this finding.
        /// </summary>
        public string Question =>
            Recommendation.Question;

        /// <summary>
        /// The action title associated with the finding.
        ///
        /// During the migration this comes from the existing observation
        /// button whenever one exists.
        ///
        /// This allows the Review All report and the left-side Workspace
        /// buttons to describe the same finding consistently.
        /// </summary>
        public string ActionTitle =>
            Observation?.ActionTitle ?? Title;
    }
}