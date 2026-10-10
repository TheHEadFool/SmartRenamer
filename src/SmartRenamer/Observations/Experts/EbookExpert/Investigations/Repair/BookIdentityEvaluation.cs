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

        /// <summary>
        /// True when local identity evidence contains an explicit conflict
        /// that Scout must not resolve automatically. External research must
        /// not be used to choose between conflicting user-supplied identities.
        /// </summary>
        public bool RequiresClarification { get; init; }

        public BookIdentityCandidate? Candidate { get; init; }

        public SeriesEvidenceEvaluation? SeriesEvaluation { get; init; }

        public string Reason { get; init; } = "";

        public List<string> Evidence { get; } = new();
    }
}
