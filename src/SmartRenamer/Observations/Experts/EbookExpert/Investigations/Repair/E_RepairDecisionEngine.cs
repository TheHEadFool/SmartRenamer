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
    /// Scout confidence policy:
    ///
    ///     90% - 100%
    ///         Candidate is eligible for automatic repair when automatic
    ///         repair authorization is enabled.
    ///
    ///     70% - <90%
    ///         User decision required.
    ///
    ///     50% - <70%
    ///         Set aside for end-of-expedition review.
    ///
    ///     0% - <50%
    ///         Set aside for end-of-expedition review.
    ///
    /// Confidence and authorization are separate decisions:
    ///
    ///     Confidence answers:
    ///         "Can Scout safely determine the answer?"
    ///
    ///     Automatic authorization answers:
    ///         "Has the user allowed Scout to apply such a determination
    ///          automatically?"
    ///
    /// The decision engine does not perform the repair. It determines whether
    /// the repair service may proceed automatically, whether Scout needs the
    /// user's judgment, or whether the opportunity should be deferred.
    ///
    /// =========================================================================
    /// </summary>
    public sealed class E_RepairDecisionEngine
    {
        /// <summary>
        /// Existing public safety floor retained for compatibility with the
        /// Ebook Expert action dispatcher.
        ///
        /// Candidates below this level are never treated as immediately
        /// actionable.
        /// </summary>
        public const double MinimumConfidenceThreshold = 0.50;

        /// <summary>
        /// Confidence at or above which Scout may automatically apply a
        /// repair when automatic repair authorization is enabled.
        /// </summary>
        public const double AutomaticConfidenceThreshold = 0.90;

        /// <summary>
        /// Confidence at or above which Scout should ask the user to decide.
        /// </summary>
        public const double UserDecisionConfidenceThreshold = 0.70;

        /// <summary>
        /// Confidence below this level is not sufficient for an immediate
        /// user-facing decision.
        ///
        /// Opportunities in this range are set aside for end-of-expedition
        /// review.
        /// </summary>
        public const double ReviewConfidenceThreshold = 0.50;

        /// <summary>
        /// Evaluates a set of candidates for the current repair opportunity.
        /// </summary>
        /// <param name="candidates">
        /// Candidates produced by domain-specific research.
        /// </param>
        /// <param name="confidenceThreshold">
        /// Existing threshold supplied by the repair action pipeline.
        /// The Scout confidence policy establishes the actual decision bands.
        /// </param>
        /// <param name="automaticAuthorization">
        /// Indicates whether the user has authorized Scout to automatically
        /// apply qualifying repairs.
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

            //---------------------------------------------------------
            // Preserve the existing API parameter, but use Scout's
            // confidence policy rather than the old single threshold.
            //
            // Candidates are ordered from highest confidence to lowest.
            // The highest-confidence candidate is therefore the candidate
            // Scout evaluates for automatic repair.
            //---------------------------------------------------------

            var orderedCandidates =
                candidates
                    .OrderByDescending(
                        candidate => candidate.Confidence)
                    .ToList();

            //---------------------------------------------------------
            // 90% - 100%
            //
            // Confidence establishes that Scout can safely determine
            // the candidate.
            //
            // Authorization establishes whether Scout is currently
            // permitted to apply that determination automatically.
            //
            // IsPreferred is deliberately NOT an additional gate here.
            // A 100% confidence candidate must not regress to a waiting
            // state merely because the research provider did not mark it
            // as preferred.
            //---------------------------------------------------------

            List<RepairDecisionCandidate> preferredAutomaticCandidates =
                orderedCandidates
                    .Where(candidate =>
                        candidate.Confidence >= AutomaticConfidenceThreshold &&
                        candidate.IsPreferred)
                    .ToList();

            if (preferredAutomaticCandidates.Count == 1)
            {
                if (automaticAuthorization)
                {
                    return new RepairDecisionResult
                    {
                        State =
                            RepairRecommendation.RepairDecisionState
                                .SafeToApply,

                        SelectedCandidate =
                            preferredAutomaticCandidates[0],

                        Candidates =
                            new[] { preferredAutomaticCandidates[0] }
                    };
                }

                //-----------------------------------------------------
                // The evidence is strong enough for Scout to know the
                // answer, but automatic repair is currently disabled.
                //-----------------------------------------------------

                return new RepairDecisionResult
                {
                    State =
                        RepairRecommendation.RepairDecisionState
                            .UserDecisionRequired,

                    Candidates =
                        new[] { preferredAutomaticCandidates[0] }
                };
            }

            if (orderedCandidates[0].Confidence >=
                AutomaticConfidenceThreshold)
            {
                //-----------------------------------------------------
                // Multiple high-confidence candidates remain, or none of
                // them has been established as preferred. Confidence alone
                // is not permission to guess.
                //-----------------------------------------------------

                List<RepairDecisionCandidate> highConfidenceCandidates =
                    orderedCandidates
                        .Where(candidate =>
                            candidate.Confidence >=
                            UserDecisionConfidenceThreshold)
                        .ToList();

                return new RepairDecisionResult
                {
                    State =
                        RepairRecommendation.RepairDecisionState
                            .UserDecisionRequired,

                    Candidates =
                        highConfidenceCandidates
                };
            }

            //---------------------------------------------------------
            // 70% - <90%
            //
            // Scout has useful evidence but should not choose on the
            // user's behalf.
            //
            // Present the qualifying candidates and ask for a decision.
            //---------------------------------------------------------

            if (orderedCandidates[0].Confidence >=
                UserDecisionConfidenceThreshold)
            {
                var userDecisionCandidates =
                    orderedCandidates
                        .Where(candidate =>
                            candidate.Confidence >=
                                UserDecisionConfidenceThreshold)
                        .ToList();

                return new RepairDecisionResult
                {
                    State =
                        RepairRecommendation.RepairDecisionState
                            .UserDecisionRequired,

                    Candidates =
                        userDecisionCandidates
                };
            }

            //---------------------------------------------------------
            // Below 70%
            //
            // Do not interrupt the expedition.
            //
            // These opportunities are retained as insufficient evidence
            // for immediate action. The later end-of-expedition review
            // can collect them for the user's blanket decision.
            //---------------------------------------------------------

            return new RepairDecisionResult
            {
                State =
                    RepairRecommendation.RepairDecisionState
                        .InsufficientEvidence,

                Candidates =
                    orderedCandidates
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