using System;

namespace Scout.Observations.Conversation
{
    /// <summary>
    /// =========================================================================
    /// CV_ActionRequest
    /// =========================================================================
    ///
    /// Represents a user's request to execute an action through the
    /// Conversation Framework.
    ///
    /// The request supports two valid forms:
    ///
    /// 1. Recommendation action
    ///    - RecommendationId identifies the recommendation.
    ///    - ActionId identifies the action.
    ///
    /// 2. Selected action option
    ///    - ActionId identifies the action.
    ///    - OptionId identifies the selected result.
    ///    - ContextId identifies the domain object associated with that result.
    ///
    /// The Conversation Framework does not interpret domain-specific meaning.
    /// The appropriate Expert does that.
    ///
    /// =========================================================================
    /// </summary>
    public sealed class CV_ActionRequest
    {
        /// <summary>
        /// Identity of the recommendation from which this action originated,
        /// when the request came from a recommendation.
        /// </summary>
        public Guid RecommendationId { get; init; }

        /// <summary>
        /// Stable identifier for the domain action being requested.
        /// </summary>
        public string ActionId { get; init; } = string.Empty;

        /// <summary>
        /// Original user input that caused the action request.
        ///
        /// This is preserved for conversation history,
        /// diagnostics, and future intent analysis.
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
        ///
        /// A request is valid when it has an ActionId and either:
        ///
        /// - a RecommendationId, for a normal recommendation action, or
        /// - an OptionId, for a selected action result.
        /// </summary>
        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ActionId) &&
            (RecommendationId != Guid.Empty ||
             !string.IsNullOrWhiteSpace(OptionId));
    }
}