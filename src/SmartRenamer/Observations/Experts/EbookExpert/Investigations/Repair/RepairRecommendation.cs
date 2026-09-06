namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// =========================================================================
    /// RepairRecommendation
    /// =========================================================================
    ///
    /// Represents a repair opportunity that the Ebook Expert believes can be
    /// acted upon.
    ///
    /// This class describes the current decision state of a possible repair.
    /// It does NOT perform the repair.
    ///
    /// The decision state is intentionally domain-neutral so the same repair
    /// mechanism can be used for ISBN, title, author, publisher, language,
    /// description, cover, and other ebook repair types.
    ///
    /// =========================================================================
    /// </summary>
    public sealed class RepairRecommendation
    {
        /// <summary>
        /// The underlying facts that caused this recommendation.
        /// </summary>
        public RepairOpportunity Opportunity { get; init; } = null!;

        /// <summary>
        /// Human-readable description of what could be done.
        /// </summary>
        public string Description { get; init; } = "";

        /// <summary>
        /// Indicates whether additional information must be obtained
        /// before the repair can be prepared.
        /// </summary>
        public bool RequiresResearch { get; init; }

        /// <summary>
        /// Indicates whether the Ebook Expert believes the repair can
        /// eventually be applied safely.
        /// </summary>
        public bool IsSafeToApply { get; init; }

        /// <summary>
        /// Describes the current decision state for this repair opportunity.
        ///
        /// This state does not identify a particular candidate value.
        /// Candidate values and their evidence remain the responsibility
        /// of the domain-specific research process.
        /// </summary>
        public RepairDecisionState DecisionState { get; init; } =
            RepairDecisionState.RequiresEvaluation;

        /// <summary>
        /// =========================================================================
        /// RepairDecisionState
        /// =========================================================================
        ///
        /// Describes what the repair process currently needs to do with a
        /// repair opportunity.
        ///
        /// These states are deliberately independent of any particular
        /// repair type. They can therefore be reused for ISBN, cover,
        /// description, publisher, and future repair types.
        ///
        /// =========================================================================
        /// </summary>
        public enum RepairDecisionState
        {
            /// <summary>
            /// The opportunity has not yet been evaluated sufficiently
            /// to determine the next repair decision.
            /// </summary>
            RequiresEvaluation,

            /// <summary>
            /// The available evidence supports a safe repair decision.
            /// Whether it may be applied automatically still depends on
            /// the current repair authorization.
            /// </summary>
            SafeToApply,

            /// <summary>
            /// More than one plausible outcome remains and the user must
            /// make the decision.
            ///
            /// This applies only to this repair opportunity. It does not
            /// mean the EPUB or the entire expedition must stop.
            /// </summary>
            UserDecisionRequired,

            /// <summary>
            /// The available evidence is not sufficient to support a safe
            /// repair decision.
            ///
            /// The repair opportunity can be deferred while other repair
            /// opportunities and other EPUBs continue to be processed.
            /// </summary>
            InsufficientEvidence,

            /// <summary>
            /// The repair opportunity has been deliberately deferred for
            /// later handling.
            ///
            /// Deferral applies to this opportunity, not automatically to
            /// the entire EPUB.
            /// </summary>
            Deferred
        }
    }
}