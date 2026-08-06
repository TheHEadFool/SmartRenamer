using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Blocks;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations
{
    /// <summary>
    /// Coordinates every metadata-related investigation performed by
    /// the Ebook Expert.
    /// </summary>
    public class E_MetadataInvestigation
    {
        private readonly E_MetadataBlock _block = new();

        /// <summary>
        /// Executes the metadata investigation and returns the
        /// findings Scout should know about.
        /// </summary>
        public MetadataReport Investigate(
            IReadOnlyList<FileContext> files)
        {
            List<ExpertFinding> findings = new();

            // Private bookkeeping for this investigation.
            
            // Ask the specialist to investigate.
            MetadataReport report =
                _block.Analyze(files);



            // TODO:
            // Populate MetadataReport from the specialist's findings.
            // This report remains private to the Ebook Expert.


            return report;
        }
    }
}