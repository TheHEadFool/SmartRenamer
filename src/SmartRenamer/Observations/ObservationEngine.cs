using System.Collections.Generic;
using SmartRenamer.Models;

namespace SmartRenamer.Observations
{
    /// <summary>
    /// =========================================================================
    /// ObservationEngine
    /// =========================================================================
    ///
    /// Hosts all Observation Experts.
    ///
    /// The ObservationEngine knows which Experts are available.
    /// It asks each Expert to investigate the supplied files and
    /// gathers every ExpertFinding into a single collection.
    ///
    /// =========================================================================
    /// </summary>
    public sealed class ObservationEngine
    {
        private static readonly IReadOnlyList<ObservationExpert> _experts =
        [
            new MusicExpert(),
            new EbookExpert()
        ];

        public List<ExpertFinding> Observe(
            IReadOnlyList<FileContext> files)
        {
            List<ExpertFinding> findings = new();

            foreach (ObservationExpert expert in _experts)
            {
                findings.AddRange(
                    expert.Investigate(files));
            }

            return findings;
        }
    }
}