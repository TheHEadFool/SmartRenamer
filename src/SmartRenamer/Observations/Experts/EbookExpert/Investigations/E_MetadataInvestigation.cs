using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;
using SmartRenamer.Observations.Specialists;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations
{
    /// <summary>
    /// Coordinates every metadata-related investigation performed by
    /// the Ebook Expert.
    /// </summary>
    public class E_MetadataInvestigation
    {
        private readonly E_EbookMetadataSpecialist _specialist = new();

        /// <summary>
        /// Executes the metadata investigation and returns the
        /// findings Scout should know about.
        /// </summary>
        public List<ExpertFinding> Investigate(
            IReadOnlyList<FileContext> files)
        {
            List<ExpertFinding> findings = new();

            // Private bookkeeping for this investigation.
            MetadataReport report = new();

            // Ask the specialist to investigate.
            ExpertFinding finding = _specialist.Observe(files);

            if (!finding.FoundSomething)
            {
                return findings;
            }

            // TODO:
            // Populate MetadataReport from the specialist's findings.
            // This report remains private to the Ebook Expert.

            findings.Add(finding);

            return findings;
        }
    }
}