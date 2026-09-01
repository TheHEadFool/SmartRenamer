using System.Collections.Generic;

namespace Scout.Observations.Conversation
{
    /// <summary>
    /// =========================================================================
    /// CV_ActionOption
    /// =========================================================================
    ///
    /// Represents one selectable result produced by a Conversation action.
    ///
    /// The option is domain-neutral. The domain Expert supplies the values
    /// needed to identify and interpret the option.
    ///
    /// Examples:
    ///
    ///     ISBN candidate
    ///     Cover candidate
    ///     Summary candidate
    ///
    /// =========================================================================
    /// </summary>
    public sealed class CV_ActionOption
    {
        /// <summary>
        /// Stable identifier for this option within the action result.
        /// </summary>
        public string Id { get; init; } = string.Empty;

        /// <summary>
        /// Identifier of the Conversation action that produced this option.
        /// </summary>
        public string ActionId { get; init; } = string.Empty;

        /// <summary>
        /// Identifier of the domain object associated with this option.
        ///
        /// The Conversation Framework does not interpret this value.
        /// The originating Expert does.
        /// </summary>
        public string ContextId { get; init; } = string.Empty;

        /// <summary>
        /// Human-readable value presented to the user.
        /// </summary>
        public string Label { get; init; } = string.Empty;

        /// <summary>
        /// Supporting evidence for this option.
        /// </summary>
        public List<string> Evidence { get; } = new();

        /// <summary>
        /// Confidence associated with the option.
        ///
        /// Zero means that the action does not provide a confidence value.
        /// </summary>
        public double Confidence { get; init; }

        /// <summary>
        /// External source associated with the option, when available.
        /// </summary>
        public string Source { get; init; } = string.Empty;
    }
}