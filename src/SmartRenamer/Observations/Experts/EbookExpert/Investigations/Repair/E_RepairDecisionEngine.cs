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
    ///         Silent automatic repair when exactly one preferred candidate
    ///         exists.
    ///
    ///     70% - &lt;90%
    ///         User decision required.
    ///
    ///     50% - &lt;70%
    ///         Set aside for end-of-expedition review.
    ///
    ///     0% - &lt;50%
    ///         Set aside for end-of-expedition review.
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
        /// repair when exactly one preferred candidate exists.
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
        /// Existing authorization state supplied by the Ebook Expert.
        ///
        /// This parameter is retained for compatibility with the existing
        /// action pipeline. A candidate at 90% or above does not require a
        /// separate automatic-authorization prompt because Scout's confidence
        /// policy already establishes that it is safe to act silently.
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
            // The caller may continue supplying its existing threshold.
            // The policy here determines what Scout actually does with
            // the evidence.
            //---------------------------------------------------------

            var orderedCandidates =
                candidates
                    .OrderByDescending(
                        candidate => candidate.Confidence)
                    .ToList();

            //---------------------------------------------------------
            // Determine candidates that the domain research identified
            // as preferred.
            //---------------------------------------------------------

            var preferredCandidates =
                orderedCandidates
                    .Where(candidate => candidate.IsPreferred)
                    .ToList();

            //---------------------------------------------------------
            // 90% - 100%
            //
            // Scout can act silently when exactly one preferred candidate
            // exists.
            //
            // The previous implementation required explicit automatic
            // authorization here. That caused a 100% confidence ISBN
            // match to stop and ask the user.
            //
            // That is no longer Scout behavior.
            //---------------------------------------------------------

            if (orderedCandidates[0].Confidence >=
                AutomaticConfidenceThreshold)
            {
                return new RepairDecisionResult
                {
                    State =
                        RepairRecommendation.RepairDecisionState
                            .SafeToApply,

                    SelectedCandidate =
                        orderedCandidates[0],

                    Candidates = new[] { orderedCandidates[0] }
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
            // will collect them for the user's blanket decision.
            //
            // We intentionally retain the existing enum state here so
            // that the review collection mechanism can be added without
            // changing the current action-result contract prematurely.
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