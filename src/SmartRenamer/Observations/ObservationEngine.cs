using System;
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
    /// ACTION ARCHITECTURE
    /// =========================================================================
    ///
    /// The ObservationEngine also provides the integration boundary for
    /// domain actions requested through the Conversation Framework.
    ///
    ///     CV_ActionRequest
    ///          │
    ///          ↓
    ///     ObservationEngine
    ///          │
    ///          ↓
    ///     Domain Expert
    ///          │
    ///          ↓
    ///     Domain Action Dispatcher
    ///          │
    ///          ↓
    ///     CV_ActionResult
    ///
    /// The ObservationEngine does not interpret ActionId values.
    ///
    /// It simply gives each registered Expert an opportunity to execute the
    /// requested action.
    ///
    /// This allows the same action infrastructure to support future domains
    /// such as:
    ///
    ///     EbookExpert
    ///         ResearchMissingIsbn
    ///         ResearchMissingCover
    ///         ResearchMissingSummary
    ///
    ///     MusicExpert
    ///         Future music actions
    ///
    ///     PhotoExpert
    ///         Future photo actions
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
            IReadOnlyList<FileContext> files,
            string sourceFolderPath)
        {
            ArgumentNullException.ThrowIfNull(files);
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceFolderPath);

            List<ExpertFinding> allFindings = new();

            List<CV_Recommendation> recommendations = new();

            //---------------------------------------------------------
            // Each Expert investigates exactly once.
            //---------------------------------------------------------

            foreach (ObservationExpert expert in _experts)
            {
                expert.BeginProject(
                    sourceFolderPath,
                    files);

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
                    expert.BuildRecommendations(
                        expertFindings));
            }

            //---------------------------------------------------------
            // Make the findings available to the existing UI bridge.
            //---------------------------------------------------------

            Findings = allFindings;

            return recommendations;
        }

        //---------------------------------------------------------
        // Domain Actions
        //---------------------------------------------------------

        /// <summary>
        /// Routes a generic Conversation Framework action request to the
        /// registered Expert capable of performing the action.
        ///
        /// The ObservationEngine does not interpret the action.
        /// Each Expert owns the meaning and execution of its own ActionIds.
        ///
        /// The first successful result is returned.
        ///
        /// This provides one reusable action gateway for all future
        /// Observation Experts.
        /// </summary>
        public CV_ActionResult ExecuteAction(
            CV_ActionRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            //---------------------------------------------------------
            // Give every registered Expert an opportunity to handle
            // the action.
            //---------------------------------------------------------

            foreach (ObservationExpert expert in _experts)
            {
                CV_ActionResult result =
                    expert.ExecuteAction(request);

                if (result.Success)
                    return result;
            }

            //---------------------------------------------------------
            // No Expert handled the request.
            //---------------------------------------------------------

            return new CV_ActionResult
            {
                ActionId = request.ActionId,

                Success = false,

                Message =
                    $"No registered Expert could execute action " +
                    $"'{request.ActionId}'."
            };
        }
    }
}