using Scout.Observations.Experts.EbookExpert.Data;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// =========================================================================
    /// RepairOpportunity
    /// =========================================================================
    ///
    /// Represents one ebook and the specific repair opportunities discovered
    /// for that ebook.
    ///
    /// This class contains facts only.
    /// It does not decide whether a repair should be performed.
    /// =========================================================================
    /// </summary>
    public sealed class RepairOpportunity
    {
        public MetadataRecord Record { get; init; } = null!;

        public bool MissingTitle { get; init; }

        public bool MissingAuthor { get; init; }

        public bool MissingIsbn { get; init; }

        public bool MissingPublisher { get; init; }

        public bool MissingLanguage { get; init; }

        public bool MissingDescription { get; init; }

        public bool MissingCover { get; init; }

        /// <summary>
        /// The Phase 2 identity evaluation, when local evidence indicates that
        /// populated metadata is assigned to the wrong identity fields.
        /// The evaluation is evidence for the repair loop; it does not approve
        /// or perform a repair.
        /// </summary>
        public BookIdentityEvaluation? IdentityEvaluation { get; set; }

        /// <summary>
        /// Phase 1 or Phase 2 found evidence requiring metadata reconciliation.
        /// </summary>
        /// <remarks>
        /// Phase 1 conflicts come from MetadataReconciliation. Phase 2 identity
        /// findings are retained on IdentityEvaluation. Keeping both paths behind
        /// this one property preserves a single repair-loop completion gate.
        /// </remarks>
        public bool NeedsMetadataReconciliation =>
            Record.Reconciliation.HasConflictingFields ||
            (IdentityEvaluation?.RepairRequired ?? false);

        /// <summary>
        /// True when this ebook has no remaining missing metadata fields
        /// identified by the Repair Block.
        ///
        /// This is a factual completion state. It does not approve or
        /// perform any repair.
        /// </summary>
        public bool IsComplete =>
            !MissingTitle &&
            !MissingAuthor &&
            !MissingIsbn &&
            !MissingPublisher &&
            !MissingLanguage &&
            !MissingDescription &&
            !MissingCover &&
            !NeedsMetadataReconciliation;
    }
}