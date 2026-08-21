using System;

namespace Scout.Observations.Conversation
{
    /// =========================================================================
    /// CV_ConversationMessage
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Capture one step of the conversation."
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Represents one complete conversational unit between Scout and the user.
    ///
    /// The Conversation Planner decides what Scout should communicate.
    /// This class records the resulting conversational message.
    ///
    /// -------------------------------------------------------------------------
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Record who produced the message.
    /// • Record what was said.
    /// • Preserve conversational order.
    /// • Associate the message with a recommendation when appropriate.
    /// • Identify information that must not be discarded.
    ///
    /// -------------------------------------------------------------------------
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Decide what Scout should say.
    /// • Select recommendations.
    /// • Interpret user intent.
    /// • Analyze files.
    /// • Render the UI.
    ///
    /// Those responsibilities belong to the Conversation Planner,
    /// Conversation Engine, Experts, and User Interface respectively.
    ///
    /// =========================================================================
    public sealed class CV_ConversationMessage
    {
        /// <summary>
        /// Identifies who produced this message.
        /// </summary>
        public CV_MessageRole Role { get; init; }

        /// <summary>
        /// The actual conversational content.
        /// </summary>
        public string Text { get; init; } = string.Empty;

        /// <summary>
        /// Optional identity of the recommendation associated with this message.
        ///
        /// This allows conversation history to remain connected to the same
        /// recommendation used by Review All and the Workspace.
        /// </summary>
        public Guid? RecommendationId { get; init; }

        /// <summary>
        /// Indicates that this information is important enough that the
        /// Conversation Planner should preserve it when deciding what to show.
        ///
        /// This is deliberately a property of the message rather than the UI.
        /// The Planner decides whether information is critical; the UI merely
        /// displays the resulting message.
        /// </summary>
        public bool IsCritical { get; init; }

        /// <summary>
        /// Position of this message within the conversation.
        /// </summary>
        public int Sequence { get; init; }
    }

    /// <summary>
    /// Identifies the participant who produced a conversation message.
    /// </summary>
    public enum CV_MessageRole
    {
        Scout,
        User
    }
}