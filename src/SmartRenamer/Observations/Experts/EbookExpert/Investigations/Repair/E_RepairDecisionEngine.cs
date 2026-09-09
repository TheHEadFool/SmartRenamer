using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scout.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// Evaluates research candidates for an Ebook repair opportunity.
    ///
    /// This engine is generic across Ebook repair types. It does not know
    /// whether the repair concerns an ISBN, title, author, publisher,
    /// language, description, cover, or another Ebook value.
    ///
    /// Domain-specific research supplies the candidates, their evidence,
    /// confidence, and whether a candidate has been established as preferred.
    ///
    /// This engine evaluates the decision state. It does not perform research,
    /// modify files, communicate with Scout, or manage the EPUB repair queue.
    /// </summary>
    public sealed class E_RepairDecisionEngine
    {
        /// <summary>
        /// The minimum confidence allowed by the Ebook repair safety rule.
        /// Scout must never automatically accept a candidate below this floor.
        /// </summary>
        public const double MinimumConfidenceThreshold = 0.50;

        /// <summary>
        /// Evaluates a set of candidates for the current repair opportunity.
        /// </summary>
        /// <param name="candidates">
        /// Candidates produced by domain-specific research.
        /// </param>
        /// <param name="confidenceThreshold">
        /// The confidence threshold currently being used by the repair
        /// expedition.
        /// </param>
        /// <param name="automaticAuthorization">
        /// True when the user has explicitly authorized Scout to make
        /// qualifying repair decisions.
        /// </param>
        public RepairDecisionResult Evaluate(
            IReadOnlyList<RepairDecisionCandidate> candidates,
            double confidenceThreshold,
            bool automaticAuthorization)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return new RepairDecisionResult
                {
                    State = RepairRecommendation.RepairDecisionState.InsufficientEvidence
                };
            }

            double threshold = Math.Max(
                MinimumConfidenceThreshold,
                confidenceThreshold);

            var qualifyingCandidates = candidates
                .Where(candidate => candidate.Confidence >= threshold)
                .OrderByDescending(candidate => candidate.Confidence)
                .ToList();

            if (qualifyingCandidates.Count == 0)
            {
                return new RepairDecisionResult
                {
                    State = RepairRecommendation.RepairDecisionState.InsufficientEvidence
                };
            }

            if (!automaticAuthorization)
            {
                return new RepairDecisionResult
                {
                    State = RepairRecommendation.RepairDecisionState.UserDecisionRequired,
                    Candidates = qualifyingCandidates
                };
            }

            var preferredCandidates = qualifyingCandidates
                .Where(candidate => candidate.IsPreferred)
                .ToList();

            if (preferredCandidates.Count == 1)
            {
                return new RepairDecisionResult
                {
                    State = RepairRecommendation.RepairDecisionState.SafeToApply,
                    SelectedCandidate = preferredCandidates[0],
                    Candidates = qualifyingCandidates
                };
            }

            return new RepairDecisionResult
            {
                State = RepairRecommendation.RepairDecisionState.UserDecisionRequired,
                Candidates = qualifyingCandidates
            };
        }
    }

    /// <summary>
    /// Contains the result of evaluating repair candidates.
    /// </summary>
    public sealed class RepairDecisionResult
    {
        /// <summary>
        /// The decision state determined by the decision engine.
        /// </summary>
        public RepairRecommendation.RepairDecisionState State { get; init; }

        /// <summary>
        /// The candidate selected automatically when the decision is safe.
        /// </summary>
        public RepairDecisionCandidate? SelectedCandidate { get; init; }

        /// <summary>
        /// Candidates that remain relevant to the decision.
        /// </summary>
        public IReadOnlyList<RepairDecisionCandidate> Candidates { get; init; } =
            Array.Empty<RepairDecisionCandidate>();
    }
}