using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations;

namespace SmartRenamer.Observations.Specialists
{
    /// <summary>
    /// A Specialist is responsible for one domain of expertise
    /// within an Expert's knowledge.
    ///
    /// Specialists never communicate with Scout directly.
    /// They investigate their domain and report factual
    /// findings back to their Expert.
    ///
    /// Experts decide which findings are important enough
    /// to become ProjectObservations.
    /// </summary>
    public abstract class ObservationSpecialist
    {
        /// <summary>
        /// Display name.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Short description of what this specialist investigates.
        /// </summary>
        public abstract string Summary { get; }

        /// <summary>
        /// Investigate the supplied files and report what was found.
        /// </summary>
        public abstract ExpertFinding Observe(
            IReadOnlyList<FileContext> files);
    }
}