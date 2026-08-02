using System.Collections.Generic;
using SmartRenamer.Models;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations
{
    /// <summary>
    /// =========================================================================
    /// E_EnrichmentInvestigation
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Coordinates enrichment-related investigations performed by the
    /// Ebook Expert.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Coordinate enrichment Consultants.
    /// • Discover additional information that can improve the library.
    /// • Locate online metadata sources.
    /// • Locate alternative cover artwork.
    /// • Discover series, author, and publisher information.
    /// • Collect observations.
    /// • Report findings back to the Ebook Expert.
    ///
    /// This Investigation does NOT
    /// -------------------------------------------------------------------------
    /// • Download information automatically.
    /// • Modify ebook files.
    /// • Communicate with Scout.
    ///
    /// Those responsibilities belong to Consultants and Blocks.
    /// =========================================================================
    /// </summary>
    public class E_EnrichmentInvestigation
    {
        public List<ExpertFinding> Investigate(
            IReadOnlyList<FileContext> files)
        {
            List<ExpertFinding> findings = new();

            // Enrichment investigation has not yet been implemented.
            // This Investigation exists as a completed architectural
            // component and will gain Consultants in future iterations.

            return findings;
        }
    }
}