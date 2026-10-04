using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using System.Collections.Generic;

namespace Scout.Observations.Experts.EbookExpert.Data
{
    /// <summary>
    /// Represents one researched ebook and the file it came from.
    /// </summary>
    public sealed class MetadataRecord
    {
        public FileContext File { get; init; } = null!;

        public E_EbookMetadata Metadata { get; init; } = null!;

        /// <summary>
        /// Evidence discovered specifically for this ebook. The record is the
        /// stable handoff point so every repair step can use the evidence
        /// already gathered about this particular book.
        /// </summary>
        public List<MetadataEvidence> Evidence { get; } = new();

        /// <summary>
        /// Phase 1 metadata reconciliation for this ebook. This records what
        /// the initial evidence evaluation can establish without modifying the
        /// ebook or performing external research.
        /// </summary>
        public MetadataReconciliation Reconciliation { get; set; } = new();
    }
}