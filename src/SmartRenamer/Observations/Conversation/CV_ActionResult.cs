using System.Collections.Generic;

namespace Scout.Observations.Conversation
{
    /// <summary>
    /// =========================================================================
    /// CV_ActionResult
    /// =========================================================================
    ///
    /// Represents the result of a domain action requested through the
    /// Conversation Framework.
    ///
    /// The result is deliberately domain-neutral.
    ///
    /// An action may:
    ///
    ///     • succeed or fail
    ///     • provide explanatory information
    ///     • return selectable options
    ///     • provide supporting evidence
    ///
    /// Examples:
    ///
    ///     ResearchMissingIsbn
    ///     ResearchMissingCover
    ///     ResearchMissingSummary
    ///     RepairMissingMetadata
    ///
    /// The Conversation Framework presents the result.
    /// The domain Expert remains responsible for interpreting domain meaning
    /// and performing subsequent domain operations.
    ///
    /// =========================================================================
    /// </summary>
    public sealed class CV_ActionResult
    {
        /// <summary>
        /// Stable identifier of the action that was requested.
        /// </summary>
        public string ActionId { get; init; } = string.Empty;

        /// <summary>
        /// Indicates whether the action completed successfully.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Indicates that the completed action changed the underlying
        /// file state and the current project should be observed again.
        /// </summary>
        public bool RequiresReobservation { get; init; }


        /// <summary>
        /// Human-readable description of the result.
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// General supporting evidence produced by the action.
        /// </summary>
        public List<string> Evidence { get; } = new();

        /// <summary>
        /// Structured options produced by the action.
        ///
        /// These can be presented to the user for selection.
        ///
        /// Examples:
        ///
        ///     ISBN candidates
        ///     Cover candidates
        ///     Summary candidates
        /// </summary>
        public List<CV_ActionOption> Options { get; } = new();

        /// <summary>
        /// Indicates whether the action produced useful information.
        /// </summary>
        public bool HasResult =>
            Success ||
            !string.IsNullOrWhiteSpace(Message) ||
            Evidence.Count > 0 ||
            Options.Count > 0;
    }
}