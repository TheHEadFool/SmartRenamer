using System;

namespace Scout.Observations.Conversation
{
    /// <summary>
    /// =========================================================================
    /// CV_ActionRequest
    /// =========================================================================
    ///
    /// Represents a user's decision to invoke the next-step action associated
    /// with the recommendation currently being discussed.
    ///
    /// This is deliberately generic.
    ///
    /// The Conversation Framework knows WHAT action the user approved.
    /// The domain Expert knows HOW that action is performed.
    ///
    /// Examples:
    ///
    ///     ResearchMissingIsbn
    ///     ResearchMissingCover
    ///     ResearchMissingSummary
    ///
    /// The ActionId is therefore a routing key, not an implementation.
    ///
    /// =========================================================================
    /// </summary>
    public sealed class CV_ActionRequest
    {
        /// <summary>
        /// Identity of the recommendation from which this action originated.
        ///
        /// This preserves the connection to the original ExpertFinding.
        /// </summary>
        public Guid RecommendationId { get; init; }

        /// <summary>
        /// Stable identifier for the domain action being requested.
        /// </summary>
        public string ActionId { get; init; } = string.Empty;

        /// <summary>
        /// Original user input that caused the action request.
        ///
        /// This is preserved for conversation history, diagnostics,
        /// and future intent analysis.
        /// </summary>
        public string UserInput { get; init; } = string.Empty;

        /// <summary>
        /// Indicates whether this request identifies a usable action.
        /// </summary>
        public bool IsValid =>
            RecommendationId != Guid.Empty &&
            !string.IsNullOrWhiteSpace(ActionId);
    }
}
