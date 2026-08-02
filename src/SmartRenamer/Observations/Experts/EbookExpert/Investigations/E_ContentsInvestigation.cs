using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants;
using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations
{
    /// <summary>
    /// =========================================================================
    /// E_ContentsInvestigation
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Coordinates content-related investigations performed by the
    /// Ebook Expert.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Coordinate content Consultants.
    /// • Investigate covers, chapters, tables of contents, and other
    ///   embedded ebook content.
    /// • Collect observations.
    /// • Report findings back to the Ebook Expert.
    ///
    /// This Investigation does NOT
    /// -------------------------------------------------------------------------
    /// • Read ebook files directly.
    /// • Modify ebook content.
    /// • Communicate with Scout.
    ///
    /// Those responsibilities belong to Consultants and Blocks.
    /// =========================================================================
    /// </summary>
    public class E_ContentsInvestigation
    {
        //---------------------------------------------------------
        // Consultants
        //---------------------------------------------------------
        // Each Consultant investigates one specific aspect of the
        // ebook's contents. Consultants never communicate directly
        // with Scout and never modify files.
        //---------------------------------------------------------

        private readonly TableOfContentsConsultant _tableOfContents = new();

        //---------------------------------------------------------

        public List<ExpertFinding> Investigate(
            IReadOnlyList<FileContext> files)
        {
            List<ExpertFinding> findings = new();

            findings.Add(
                _tableOfContents.Observe(files));

            return findings;
        }
    }
}