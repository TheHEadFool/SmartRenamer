using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// Describes what the currently available local evidence can establish
    /// about an ebook's Series and SeriesNumber.
    ///
    /// This is an evaluation of evidence only. It does not approve or modify
    /// an EPUB.
    /// </summary>
    public sealed class SeriesEvidenceEvaluation
    {
        public SeriesEvidenceState State { get; init; }

        public string CandidateSeries { get; init; } = "";

        public string CandidateSeriesNumber { get; init; } = "";

        public string Reason { get; init; } = "";

        public List<string> Evidence { get; } = new();
    }

    public enum SeriesEvidenceState
    {
        NoSignal,
        Resolved,
        Unresolved,
        Conflicting
    }
}
