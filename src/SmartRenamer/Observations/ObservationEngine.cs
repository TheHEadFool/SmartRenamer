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
    /// DISCOVERY ARCHITECTURE
    /// =========================================================================
    ///
    /// Experts may optionally declare discovery requirements through the
    /// generic ExpertDiscoveryRequest contract.
    ///
    /// The ObservationEngine collects those requests from the registered
    /// Experts.
    ///
    /// IMPORTANT
    /// -------------------------------------------------------------------------
    /// The ObservationEngine does not interpret domain-specific extensions.
    ///
    /// An Expert's discovery request describes candidate files that may be
    /// relevant to that Expert. The Expert remains responsible for deciding
    /// whether a candidate actually belongs to its domain.
    ///
    /// This allows Scout to move toward one physical scan shared by all
    /// Experts without placing domain knowledge into FolderScanner.
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
        // Discovery Requirements
        //---------------------------------------------------------
        //
        // Every registered Expert may optionally provide a discovery
        // request. The ObservationEngine collects those requests without
        // interpreting them.
        //
        // This is the first handshake between the Expert-owned discovery
        // contract and the shared Scout infrastructure.
        //
        //---------------------------------------------------------

        public IReadOnlyList<ExpertDiscoveryRequest> DiscoveryRequests
        {
            get
            {
                List<ExpertDiscoveryRequest> requests = new();

                foreach (ObservationExpert expert in _experts)
                {
                    ExpertDiscoveryRequest? request =
                        expert.DiscoveryRequest;

                    if (request != null)
                        requests.Add(request);
                }

                return requests;
            }
        }

        public IReadOnlyList<ExpertDiscoveryBinding> DiscoveryBindings
        {
            get
            {
                List<ExpertDiscoveryBinding> bindings = new();

                foreach (ObservationExpert expert in _experts)
                {
                    ExpertDiscoveryRequest? request =
                        expert.DiscoveryRequest;

                    if (request != null)
                    {
                        bindings.Add(
                            new ExpertDiscoveryBinding(
                                expert,
                                request));
                    }
                }

                return bindings;
            }
        }

        /// <summary>
        /// Applies a generic discovery choice to the registered Expert
        /// identified by its name.
        ///
        /// The ObservationEngine does not interpret the option. The selected
        /// Expert receives the option ID and determines what it means.
        /// </summary>
        public void ApplyDiscoveryChoice(
            string expertName,
            string optionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(expertName);
            ArgumentException.ThrowIfNullOrWhiteSpace(optionId);

            foreach (ObservationExpert expert in _experts)
            {
                if (string.Equals(
                        expert.Name,
                        expertName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    expert.ApplyDiscoveryChoice(optionId);
                    return;
                }
            }

            throw new ArgumentException(
                $"No registered Expert named '{expertName}' was found.",
                nameof(expertName));
        }
        //---------------------------------------------------------
        // Expert Decisions
        //---------------------------------------------------------

        /// <summary>
        /// Returns the decision requests currently supplied by registered
        /// Observation Experts.
        ///
        /// The ObservationEngine does not interpret the requests.
        /// The owning Expert determines their meaning.
        /// </summary>
        public IReadOnlyList<ExpertDecisionRequest> DecisionRequests
        {
            get
            {
                List<ExpertDecisionRequest> requests = new();

                foreach (ObservationExpert expert in _experts)
                {
                    ExpertDecisionRequest? request =
                        expert.DecisionRequest;

                    if (request != null)
                        requests.Add(request);
                }

                return requests;
            }
        }

        /// <summary>
        /// Returns bindings between decision requests and the Experts
        /// that own them.
        ///
        /// The binding allows the generic conversation infrastructure to
        /// route a user's choice back to the correct Expert.
        /// </summary>
        public IReadOnlyList<ExpertDecisionBinding> DecisionBindings
        {
            get
            {
                List<ExpertDecisionBinding> bindings = new();

                foreach (ObservationExpert expert in _experts)
                {
                    ExpertDecisionRequest? request =
                        expert.DecisionRequest;

                    if (request != null)
                    {
                        bindings.Add(
                            new ExpertDecisionBinding(
                                expert,
                                request));
                    }
                }

                return bindings;
            }
        }

        /// <summary>
        /// Applies a generic decision choice to the registered Expert
        /// identified by its name.
        ///
        /// The ObservationEngine does not interpret the option.
        /// The selected Expert receives the opaque option ID and determines
        /// what it means.
        /// </summary>
        public void ApplyDecisionChoice(
            string expertName,
            string optionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(expertName);
            ArgumentException.ThrowIfNullOrWhiteSpace(optionId);

            foreach (ObservationExpert expert in _experts)
            {
                if (string.Equals(
                        expert.Name,
                        expertName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    expert.ApplyDecisionChoice(optionId);
                    return;
                }
            }

            throw new ArgumentException(
                $"No registered Expert named '{expertName}' was found.",
                nameof(expertName));
        }
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
        // Re-observation Completion
        //---------------------------------------------------------

        /// <summary>
        /// Gives each registered Expert an opportunity to determine
        /// whether its current domain-specific workflow is complete
        /// after a re-observation pass.
        ///
        /// The ObservationEngine does not know what "complete" means.
        /// Each Expert owns that decision.
        ///
        /// The first Expert that advances its current workflow reports
        /// success.
        /// </summary>
        public bool CompleteCurrentIfComplete()
        {
            foreach (ObservationExpert expert in _experts)
            {
                if (expert.CompleteCurrentIfComplete())
                    return true;
            }

            return false;
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