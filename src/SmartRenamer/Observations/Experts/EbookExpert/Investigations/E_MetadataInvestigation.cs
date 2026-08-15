using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Blocks;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations
{
    /// <summary>
    /// =========================================================================
    /// E_MetadataInvestigation
    /// =========================================================================
    ///
    /// PURPOSE
    /// -------------------------------------------------------------------------
    /// Coordinates the metadata investigation performed by the Ebook Expert.
    ///
    /// Metadata is an upstream research source for several other Ebook
    /// Investigations. The metadata research is therefore acquired once and
    /// the resulting MetadataReport is retained for downstream consumption.
    ///
    /// ARCHITECTURE
    /// -------------------------------------------------------------------------
    ///
    ///     Files
    ///       ↓
    ///     E_MetadataBlock
    ///       ↓
    ///     MetadataReport
    ///       ├──→ E_MetadataConsultant
    ///       │         ↓
    ///       │    ExpertFindings
    ///       │
    ///       ├──→ Contents
    ///       ├──→ Organization
    ///       ├──→ Quality
    ///       ├──→ Repair
    ///       └──→ Enrichment
    ///
    /// RESPONSIBILITIES
    /// -------------------------------------------------------------------------
    /// • Coordinate the Metadata Block.
    /// • Coordinate the Metadata Consultant.
    /// • Preserve the MetadataReport for downstream Investigations.
    /// • Preserve Metadata ExpertFindings for the Ebook Expert.
    ///
    /// DOES NOT
    /// -------------------------------------------------------------------------
    /// • Read ebook files directly.
    /// • Interpret metadata inside the Block.
    /// • Modify ebook files.
    /// • Communicate with Scout.
    ///
    /// The Block acquires facts.
    /// The Report preserves facts.
    /// The Consultant interprets those facts.
    ///
    /// =========================================================================
    /// </summary>
    public sealed class E_MetadataInvestigation
    {
        private readonly E_MetadataBlock _block = new();

        private readonly E_MetadataConsultant _consultant = new();

        /// <summary>
        /// Gets the ExpertFindings produced by the Metadata Consultant during
        /// the most recent investigation.
        ///
        /// The findings are kept separate from MetadataReport because the
        /// MetadataReport is objective research that is also consumed by
        /// downstream Investigations.
        /// </summary>
        public IReadOnlyList<ExpertFinding> Findings { get; private set; }
            = new List<ExpertFinding>();

        /// <summary>
        /// Executes the metadata investigation.
        ///
        /// The MetadataReport is returned because downstream Investigations
        /// consume the same metadata research rather than reacquiring it.
        ///
        /// The Metadata Consultant independently interprets that report and
        /// stores the resulting ExpertFindings in Findings.
        /// </summary>
        public MetadataReport Investigate(
            IReadOnlyList<FileContext> files)
        {
            //---------------------------------------------------------
            // Ask the Block to discover facts.
            //---------------------------------------------------------

            MetadataReport report =
                _block.Analyze(files);

            //---------------------------------------------------------
            // Ask the Consultant to interpret those facts.
            //---------------------------------------------------------

            Findings =
                _consultant.Review(report);

            //---------------------------------------------------------
            // The MetadataReport remains the shared research source
            // for downstream Investigations.
            //---------------------------------------------------------

            return report;
        }
    }
}