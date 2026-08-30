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
        /// Identifier of a specific result selected by the user.
        ///
        /// The Conversation Framework treats this as an opaque identifier.
        /// It does not know whether the identifier represents an ISBN,
        /// a cover, a book record, or another domain-specific result.
        /// </summary>
        public string OptionId { get; init; } = string.Empty;

        /// <summary>
        /// Domain context associated with the selected option.
        ///
        /// The Conversation Framework preserves this value but does not
        /// interpret it. The domain Expert may use it to identify the
        /// file or other domain object associated with the selected result.
        /// </summary>
        public string ContextId { get; init; } = string.Empty;

        /// <summary>
        /// Indicates whether this request identifies a usable action.
        /// </summary>
        public bool IsValid =>
            RecommendationId != Guid.Empty &&
            !string.IsNullOrWhiteSpace(ActionId);
    }
}