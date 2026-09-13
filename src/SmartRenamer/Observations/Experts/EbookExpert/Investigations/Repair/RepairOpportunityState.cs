using Scout.Observations.Experts.EbookExpert.Investigations.Repair;
using System;
using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// =========================================================================
    /// RepairOpportunityState
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Maintains the processing state of one specific repair opportunity for
    /// one EPUB.
    ///
    /// RepairOpportunity answers:
    ///
    ///     "What is missing?"
    ///
    /// RepairOpportunityState answers:
    ///
    ///     "What has Scout determined about that missing item so far?"
    ///
    /// One EPUB may therefore have several independent opportunity states.
    ///
    /// This class stores state only.
    /// It does not research, make decisions, modify EPUB files, or communicate
    /// with the Conversation Framework.
    /// =========================================================================
    /// </summary>
    internal sealed class RepairOpportunityState
    {
        private readonly List<RepairDecisionCandidate> _candidates = new();

        public RepairOpportunityState(
            string originalPath,
            string repairType)
        {
            if (string.IsNullOrWhiteSpace(originalPath))
                throw new ArgumentException(
                    "The original EPUB path cannot be empty.",
                    nameof(originalPath));

            if (string.IsNullOrWhiteSpace(repairType))
                throw new ArgumentException(
                    "The repair type cannot be empty.",
                    nameof(repairType));

            OriginalPath = originalPath;
            RepairType = repairType;
        }

        //---------------------------------------------------------
        // Stable opportunity identity
        //---------------------------------------------------------

        /// <summary>
        /// The original EPUB path.
        ///
        /// This remains the stable identity even if the EPUB later receives
        /// a repaired working-copy path.
        /// </summary>
        public string OriginalPath { get; }

        /// <summary>
        /// Identifies the specific repair opportunity.
        ///
        /// Examples:
        ///     ISBN
        ///     Publisher
        ///     Author
        ///     Cover
        /// </summary>
        public string RepairType { get; }

        //---------------------------------------------------------
        // Decision state
        //---------------------------------------------------------

        /// <summary>
        /// Current state of this repair opportunity.
        ///
        /// This records the result of evaluation. It does not perform the
        /// evaluation itself.
        /// </summary>
        public RepairRecommendation.RepairDecisionState DecisionState
        {
            get;
            private set;
        } = RepairRecommendation.RepairDecisionState.RequiresEvaluation;

        //---------------------------------------------------------
        // Candidates
        //---------------------------------------------------------

        /// <summary>
        /// Candidates produced by the most recent evaluation.
        /// </summary>
        public IReadOnlyList<RepairDecisionCandidate> Candidates =>
            _candidates;

        /// <summary>
        /// Candidate selected by the decision process, when one exists.
        /// </summary>
        public RepairDecisionCandidate? SelectedCandidate
        {
            get;
            private set;
        }

        //---------------------------------------------------------
        // Explanation
        //---------------------------------------------------------

        /// <summary>
        /// Reason associated with the current state.
        /// </summary>
        public string Reason
        {
            get;
            private set;
        } = string.Empty;

        //---------------------------------------------------------
        // State recording
        //---------------------------------------------------------

        /// <summary>
        /// Records the result of a decision evaluation.
        ///
        /// This method records what another component decided.
        /// It does not make the decision itself.
        /// </summary>
        public void RecordEvaluation(
            RepairDecisionResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            _candidates.Clear();

            foreach (RepairDecisionCandidate candidate
                in result.Candidates)
            {
                _candidates.Add(candidate);
            }

            DecisionState = result.State;
            SelectedCandidate = result.SelectedCandidate;
        }

        /// <summary>
        /// Records an explanation associated with this opportunity.
        /// </summary>
        public void RecordReason(string reason)
        {
            Reason = reason ?? string.Empty;
        }

        /// <summary>
        /// Records an explicitly selected candidate.
        ///
        /// The caller is responsible for determining that the selection is
        /// valid. This class only records the resulting state.
        /// </summary>
        public void RecordSelection(
            RepairDecisionCandidate candidate,
            string reason)
        {
            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));

            SelectedCandidate = candidate;

            DecisionState =
                RepairRecommendation.RepairDecisionState.SafeToApply;

            Reason = reason ?? string.Empty;
        }

        /// <summary>
        /// Records that this opportunity has been deferred.
        /// </summary>
        public void MarkDeferred(string reason)
        {
            DecisionState =
                RepairRecommendation.RepairDecisionState.Deferred;

            Reason = reason ?? string.Empty;
        }
    }
}