using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Data.Reports
{
    /// <summary>
    /// Describes the result of the first metadata-evidence evaluation for one
    /// ebook. This is research only; it does not authorize a repair.
    /// </summary>
    public sealed class MetadataReconciliation
    {
        public MetadataFieldReconciliation Title { get; } =
            new("Title");

        public MetadataFieldReconciliation Author { get; } =
            new("Author");

        public MetadataFieldReconciliation Series { get; } =
            new("Series");

        public MetadataFieldReconciliation SeriesNumber { get; } =
            new("SeriesNumber");

        /// <summary>
        /// Evidence that may help identify the book but has not yet been safely
        /// attributed to a particular metadata field. Examples include the
        /// filename and opening-content evidence.
        /// </summary>
        public List<MetadataEvidence> UninterpretedIdentityEvidence { get; } =
            new();

        /// <summary>
        /// True when the observed metadata itself is missing or directly
        /// contradicted by independent field-specific evidence.
        /// </summary>
        public bool NeedsFurtherEvaluation =>
            Title.NeedsFurtherEvaluation ||
            Author.NeedsFurtherEvaluation ||
            Series.NeedsFurtherEvaluation ||
            SeriesNumber.NeedsFurtherEvaluation;

        /// <summary>
        /// True when Phase 1 found direct independent evidence that one or more
        /// observed metadata fields conflict with the current EPUB metadata.
        /// A conflict is actionable by the Repair investigation; a merely
        /// missing Series value is not, because a standalone ebook may
        /// legitimately have no series.
        /// </summary>
        public bool HasConflictingFields =>
            Title.State == MetadataFieldReconciliationState.Conflicting ||
            Author.State == MetadataFieldReconciliationState.Conflicting ||
            Series.State == MetadataFieldReconciliationState.Conflicting ||
            SeriesNumber.State == MetadataFieldReconciliationState.Conflicting;

        /// <summary>
        /// Indicates that additional identity evidence exists which Phase 2
        /// may interpret. Its presence alone does not mean the metadata is
        /// wrong.
        /// </summary>
        public bool HasUninterpretedIdentityEvidence =>
            UninterpretedIdentityEvidence.Count > 0;
    }

    public enum MetadataFieldReconciliationState
    {
        ObservedOnly,
        Supported,
        Missing,
        Conflicting
    }

    /// <summary>
    /// Phase 1 assessment of one metadata field. Candidate values are recorded
    /// only when independent evidence actually names that field.
    /// </summary>
    public sealed class MetadataFieldReconciliation
    {
        public MetadataFieldReconciliation(string field)
        {
            Field = field;
        }

        public string Field { get; }

        public string ObservedValue { get; internal set; } = "";

        public MetadataFieldReconciliationState State { get; internal set; }
            = MetadataFieldReconciliationState.ObservedOnly;

        public List<string> CandidateValues { get; } = new();

        public List<MetadataEvidence> Evidence { get; } = new();

        public bool NeedsFurtherEvaluation =>
            State == MetadataFieldReconciliationState.Missing ||
            State == MetadataFieldReconciliationState.Conflicting;
    }
}
