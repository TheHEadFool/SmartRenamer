using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Contents.Consultants
{
    /// <summary>
    /// =========================================================================
    /// TableOfContentsConsultant
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Determines whether an EPUB contains a valid table of contents.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Examine EPUB navigation.
    /// • Determine whether a TOC exists.
    /// • Report its findings.
    ///
    /// This Consultant does NOT
    /// -------------------------------------------------------------------------
    /// • Repair navigation.
    /// • Read chapter contents.
    /// • Communicate with Scout.
    ///
    /// Those responsibilities belong to Blocks and the Investigation.
    /// =========================================================================
    /// </summary>
    public class TableOfContentsConsultant
    {
        public ExpertFinding Observe(
            IReadOnlyList<FileContext> files)
        {
            ExpertFinding finding = new();

            // Implementation will come later.

            return finding;
        }
    }
}