using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scout.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// =========================================================================
    /// E_RepairDecisionEngine
    /// =========================================================================
    ///
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
    ///
    /// =========================================================================
    /// Decision policy
    /// =========================================================================
    ///
    /// A uniquely preferred candidate at 100% confidence is safe to apply
    /// automatically. This represents a repair Scout can determine with
    /// complete certainty and should not require unnecessary user involvement.
    ///
    /// Below 100%, automatic handling requires the user's repair authorization.
    ///
    /// The confidence threshold is supplied by the active repair expedition.
    /// The expedition may progressively lower that threshold according to
    /// its repair policy.
    ///
    /// The engine never chooses between competing candidates merely because
    /// one has a higher confidence value.
    /// </summary>
    public sealed class E_RepairDecisionEngine
    {
        //---------------------------------------------------------
        // Safety floor
        //---------------------------------------------------------

        /// <summary>
        /// The minimum confidence allowed by the Ebook repair safety rule.
        ///
        /// Scout must never automatically accept a candidate below this floor.
        /// </summary>
        public const double MinimumConfidenceThreshold = 0.50;

        //---------------------------------------------------------
        // Evaluation
        //---------------------------------------------------------

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
        /// True when the user has authorized Scout to make qualifying
        /// repairs below complete certainty.
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
                    State =
                        RepairRecommendation.RepairDecisionState
                            .InsufficientEvidence
                };
            }

            double threshold = Math.Max(
                MinimumConfidenceThreshold,
                confidenceThreshold);

            List<RepairDecisionCandidate> qualifyingCandidates =
                candidates
                    .Where(candidate =>
                        candidate.Confidence >= threshold)
                    .OrderByDescending(candidate =>
                        candidate.Confidence)
                    .ToList();

            if (qualifyingCandidates.Count == 0)
            {
                return new RepairDecisionResult
                {
                    State =
                        RepairRecommendation.RepairDecisionState
                            .InsufficientEvidence
                };
            }

            //---------------------------------------------------------
            // Complete certainty
            //---------------------------------------------------------
            //
            // A uniquely preferred candidate at 100% confidence is
            // unambiguous. Scout should perform this repair without
            // asking the user for authorization.
            //
            //---------------------------------------------------------

            List<RepairDecisionCandidate> certainCandidates =
                qualifyingCandidates
                    .Where(candidate =>
                        candidate.Confidence >= 1.00)
                    .ToList();

            List<RepairDecisionCandidate> preferredCertainCandidates =
                certainCandidates
                    .Where(candidate =>
                        candidate.IsPreferred)
                    .ToList();

            if (preferredCertainCandidates.Count == 1)
            {
                return new RepairDecisionResult
                {
                    State =
                        RepairRecommendation.RepairDecisionState
                            .SafeToApply,
                    SelectedCandidate =
                        preferredCertainCandidates[0],
                    Candidates = qualifyingCandidates
                };
            }

            //---------------------------------------------------------
            // Below complete certainty
            //---------------------------------------------------------
            //
            // Automatic handling below 100% requires the user's
            // delegation. Without that delegation, Scout presents
            // the qualifying choices rather than applying them.
            //
            //---------------------------------------------------------

            if (!automaticAuthorization)
            {
                return new RepairDecisionResult
                {
                    State =
                        RepairRecommendation.RepairDecisionState
                            .UserDecisionRequired,
                    Candidates = qualifyingCandidates
                };
            }

            //---------------------------------------------------------
            // Authorized automatic repair
            //---------------------------------------------------------
            //
            // Scout may automatically apply a uniquely preferred
            // candidate at the currently authorized threshold.
            //
            //---------------------------------------------------------

            List<RepairDecisionCandidate> preferredCandidates =
                qualifyingCandidates
                    .Where(candidate =>
                        candidate.IsPreferred)
                    .ToList();

            if (preferredCandidates.Count == 1)
            {
                return new RepairDecisionResult
                {
                    State =
                        RepairRecommendation.RepairDecisionState
                            .SafeToApply,
                    SelectedCandidate =
                        preferredCandidates[0],
                    Candidates = qualifyingCandidates
                };
            }

            //---------------------------------------------------------
            // Competing candidates
            //---------------------------------------------------------
            //
            // Confidence alone does not make one candidate the winner.
            // If Scout cannot distinguish between multiple qualifying
            // candidates, the user must decide.
            //
            //---------------------------------------------------------

            return new RepairDecisionResult
            {
                State =
                    RepairRecommendation.RepairDecisionState
                        .UserDecisionRequired,
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