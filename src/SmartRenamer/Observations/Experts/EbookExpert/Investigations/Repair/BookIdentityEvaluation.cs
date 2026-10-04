using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// Result of evaluating the observed identity of one ebook.
    ///
    /// This is evidence for the repair loop, not approval to modify the EPUB.
    /// </summary>
    public sealed class BookIdentityEvaluation
    {
        public bool RepairRequired { get; init; }

        public BookIdentityCandidate? Candidate { get; init; }

        public SeriesEvidenceEvaluation? SeriesEvaluation { get; init; }

        public string Reason { get; init; } = "";

        public List<string> Evidence { get; } = new();
    }
}
