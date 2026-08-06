using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Contents;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants

// Begin namespace
{
    /// <summary>
    /// =========================================================================
    /// TableOfContentsConsultant
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Reviews the factual results produced by the Contents Block and
    /// determines whether the ebook collection has navigation issues.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Interpret ContentsReport.
    /// • Produce ExpertFindings.
    /// • Never inspect ebook files directly.
    ///
    /// This Consultant does NOT
    /// -------------------------------------------------------------------------
    /// • Read EPUB files.
    /// • Repair navigation.
    /// • Communicate with Scout.
    ///
    /// Those responsibilities belong to the Block and Investigation.
    /// =========================================================================
    /// </summary>
    public sealed class TableOfContentsConsultant

    // Begin TableOfContentsConsultant
    {
        public List<ExpertFinding> Review(
            ContentsReport report)

        // Begin Review()
        {
            List<ExpertFinding> findings = new();

            if (report.BooksWithoutTableOfContents > 0)
            {
                findings.Add(
                    new ExpertFinding
                    {
                        FoundSomething = true,
                        Summary =
                            $"{report.BooksWithoutTableOfContents} ebooks are missing a table of contents."
                    });
            }

            if (report.BooksWithTableOfContents > 0)
            {
                findings.Add(
                    new ExpertFinding
                    {
                        FoundSomething = true,
                        Summary =
                            $"{report.BooksWithTableOfContents} ebooks contain a table of contents."
                    });
            }

            return findings;

        } // End Review()

    } // End TableOfContentsConsultant

} // End namespace