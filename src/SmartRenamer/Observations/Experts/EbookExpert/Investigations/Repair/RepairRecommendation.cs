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
    /// This class describes a possible next action.
    /// It does NOT perform the repair.
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
    }
}