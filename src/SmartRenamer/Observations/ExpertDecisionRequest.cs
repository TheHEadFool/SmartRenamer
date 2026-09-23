using System;
using System.Collections.Generic;

namespace SmartRenamer.Observations
{
    /// <summary>
    /// Represents a generic user decision requested by an Observation Expert.
    ///
    /// The request contains no domain-specific meaning.
    /// The owning Expert determines what the options mean.
    /// </summary>
    public sealed class ExpertDecisionRequest
    {
        /// <summary>
        /// The question or prompt presented to the user.
        /// </summary>
        public string Question { get; init; } = "";

        /// <summary>
        /// The choices currently available to the user.
        /// </summary>
        public IReadOnlyList<ExpertDecisionOption> Options { get; init; } =
            Array.Empty<ExpertDecisionOption>();
    }
}