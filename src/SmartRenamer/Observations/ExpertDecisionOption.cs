using System;

namespace SmartRenamer.Observations
{
    /// <summary>
    /// Represents one generic decision option supplied by an Observation Expert.
    ///
    /// The Scout infrastructure treats this as opaque data.
    /// The owning Expert determines what the option means.
    /// </summary>
    public sealed class ExpertDecisionOption
    {
        public ExpertDecisionOption(
            string id,
            string label,
            ExpertDecisionInputKind inputKind = ExpertDecisionInputKind.None,
            string? description = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(label);

            Id = id;
            Label = label;
            InputKind = inputKind;
            Description = description ?? string.Empty;
        }

        /// <summary>
        /// Opaque identifier interpreted by the owning Expert.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Human-readable text presented to the user.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Optional explanation of what selecting this option will do.
        /// The Guide transports this text without interpreting it.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Describes the generic input, if any, required to complete this
        /// decision option. The owning Expert determines what the resulting
        /// value means.
        /// </summary>
        public ExpertDecisionInputKind InputKind { get; }
    }
}