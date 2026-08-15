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
    /// The ObservationEngine coordinates Experts, gathers their findings,
    /// and asks each Expert to translate those findings into
    /// conversation-ready recommendations.
    ///
    /// =========================================================================
    /// ARCHITECTURE
    /// =========================================================================
    ///
    /// The ObservationEngine is the integration boundary between the domain
    /// Experts and the rest of Scout.
    ///
    /// Each Expert produces ExpertFindings.
    ///
    /// Those SAME findings have two destinations:
    ///
    ///     ExpertFinding
    ///          │
    ///          ├──→ ObservationMapper
    ///          │       ↓
    ///          │   ProjectObservation
    ///          │       ↓
    ///          │   Existing Workspace UI
    ///          │
    ///          └──→ Expert Translator
    ///                  ↓
    ///              CV_Recommendation
    ///                  ↓
    ///              Conversation Framework
    ///
    /// This is deliberate.
    ///
    /// The UI and Conversation Framework must describe the same underlying
    /// Expert understanding of the project.
    ///
    /// =========================================================================
    /// IMPORTANT
    /// =========================================================================
    ///
    /// The ObservationEngine does NOT decide which domain is most important.
    ///
    /// Each Expert owns knowledge of its own domain and is responsible for
    /// determining whether its findings are relevant.
    ///
    /// The Conversation Framework later decides which recommendation Scout
    /// should discuss.
    ///
    /// =========================================================================
    /// MIGRATION NOTE
    /// =========================================================================
    ///
    /// The existing UI still uses ProjectObservation while the Conversation
    /// Framework uses CV_Recommendation.
    ///
    /// We deliberately support both during this migration.
    ///
    /// DO NOT remove the legacy ProjectObservation path until the new
    /// Expert-driven recommendation pipeline has been proven in the UI.
    ///
    /// =========================================================================
    /// </summary>
    public sealed class ObservationEngine
    {
        //---------------------------------------------------------
        // Domain Experts
        //---------------------------------------------------------

        private static readonly IReadOnlyList<ObservationExpert> _experts =
        [
            new MusicExpert(),
            new EbookExpert()
        ];

        //---------------------------------------------------------
        // Most Recent Findings
        //---------------------------------------------------------
        //
        // These are the factual findings produced during the most recent
        // observation pass.
        //
        // The existing ObservationMapper can convert these findings into
        // ProjectObservations for the current Workspace UI.
        //
        //---------------------------------------------------------

        public IReadOnlyList<ExpertFinding> Findings { get; private set; }
            = new List<ExpertFinding>();

        //---------------------------------------------------------
        // Observation
        //---------------------------------------------------------

        /// <summary>
        /// Runs every registered domain Expert exactly once.
        ///
        /// The findings produced during that pass are preserved and then
        /// translated into conversation recommendations.
        ///
        /// This guarantees that the UI and Conversation Framework receive
        /// the same factual Expert findings.
        /// </summary>
        public List<CV_Recommendation> Observe(
            IReadOnlyList<FileContext> files)
        {
            List<ExpertFinding> allFindings = new();
            List<CV_Recommendation> recommendations = new();

            //---------------------------------------------------------
            // Each Expert investigates exactly once.
            //---------------------------------------------------------

            foreach (ObservationExpert expert in _experts)
            {
                List<ExpertFinding> expertFindings =
                    expert.Investigate(files);

                //-----------------------------------------------------
                // Preserve the factual findings.
                //-----------------------------------------------------

                allFindings.AddRange(expertFindings);

                //-----------------------------------------------------
                // Translate those EXACT findings into recommendations.
                //-----------------------------------------------------

                recommendations.AddRange(
                    expert.BuildRecommendations(expertFindings));
            }

            //---------------------------------------------------------
            // Make the findings available to the existing UI bridge.
            //---------------------------------------------------------

            Findings = allFindings;

            return recommendations;
        }
    }
}