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
            ExpertDecisionInputKind inputKind = ExpertDecisionInputKind.None)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(label);

            Id = id;
            Label = label;
            InputKind = inputKind;
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
        /// Describes the generic input, if any, required to complete this
        /// decision option. The owning Expert determines what the resulting
        /// value means.
        /// </summary>
        public ExpertDecisionInputKind InputKind { get; }
    }
}