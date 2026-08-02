using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants
{
    /// <summary>
    /// =========================================================================
    /// TableOfContentsConsultant
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Determines whether an EPUB contains a table of contents.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Examine EPUB navigation.
    /// • Determine whether a table of contents exists.
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
            return new ExpertFinding
            {
                FoundSomething = false,
                Summary = "Table of contents investigation has not yet been implemented."
            };
        }
    }
}