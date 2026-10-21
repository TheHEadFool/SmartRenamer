using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using System.Collections.Generic;
using System;

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
        /// Adds evidence through the shared per-book contribution contract.
        /// Repeated publication of the same source/field/value/location is
        /// idempotent, while genuinely distinct evidence remains preserved.
        /// </summary>
        public MetadataEvidence AddEvidence(MetadataEvidence evidence)
        {
            if (evidence == null) throw new ArgumentNullException(nameof(evidence));

            MetadataEvidence? existing = Evidence.Find(item =>
                string.Equals(item.Source, evidence.Source, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Field, evidence.Field, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Value?.Trim(), evidence.Value?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.Location, evidence.Location, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                Evidence.Add(evidence);
                return evidence;
            }

            foreach (string file in evidence.Files)
            {
                if (!existing.Files.Exists(item => string.Equals(item, file, StringComparison.OrdinalIgnoreCase)))
                    existing.Files.Add(file);
            }

            return existing;
        }

        /// <summary>
        /// Phase 1 metadata reconciliation for this ebook. This records what
        /// the initial evidence evaluation can establish without modifying the
        /// ebook or performing external research.
        /// </summary>
        public MetadataReconciliation Reconciliation { get; set; } = new();
    }
}