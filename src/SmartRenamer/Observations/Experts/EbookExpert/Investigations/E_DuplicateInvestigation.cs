using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Duplicates;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations

// Begin namespace
{
    /// <summary>
    /// =========================================================================
    /// E_DuplicateInvestigation
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Coordinates duplicate-related investigations performed by the
    /// Ebook Expert.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Coordinate duplicate Blocks.
    /// • Coordinate duplicate Consultants.
    /// • Detect duplicate ebook files.
    /// • Detect duplicate metadata.
    /// • Collect observations.
    /// • Report findings back to the Ebook Expert.
    ///
    /// This Investigation does NOT
    /// -------------------------------------------------------------------------
    /// • Delete duplicate files.
    /// • Modify ebook files.
    /// • Communicate with Scout.
    ///
    /// Those responsibilities belong to Consultants and Blocks.
    /// =========================================================================
    /// </summary>
    public sealed class E_DuplicateInvestigation

    // Begin E_DuplicateInvestigation
    {
        public List<ExpertFinding> Investigate(
            IReadOnlyList<FileContext> files)

        // Begin Investigate()
        {
            List<ExpertFinding> findings = new();

            //---------------------------------------------------------
            // Ask the Block to discover facts.
            //---------------------------------------------------------

            E_DuplicateBlock block = new();

            E_DuplicateReport report =
                block.Analyze(files);

            //---------------------------------------------------------
            // Ask the Consultant to interpret those facts.
            //---------------------------------------------------------

            E_DuplicateConsultant consultant = new();

            findings.AddRange(
                consultant.Review(report));

            return findings;

        } // End Investigate()

    } // End E_DuplicateInvestigation

} // End namespace