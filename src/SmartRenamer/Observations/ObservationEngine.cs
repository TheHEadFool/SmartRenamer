using System.Collections.Generic;
using Scout.Observations.Conversation;
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
    /// Scout communicates only with the ObservationEngine.
    /// The ObservationEngine coordinates Experts, gathers their
    /// findings, then asks each Expert to translate those findings
    /// into conversation-ready recommendations.
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

        public List<CV_Recommendation> Observe(
            IReadOnlyList<FileContext> files)
        {
            List<CV_Recommendation> recommendations = new();

            foreach (ObservationExpert expert in _experts)
            {
                List<ExpertFinding> findings =
                    expert.Investigate(files);

                recommendations.AddRange(
                    expert.BuildRecommendations(findings));
            }

            return recommendations;
        }
    }
}