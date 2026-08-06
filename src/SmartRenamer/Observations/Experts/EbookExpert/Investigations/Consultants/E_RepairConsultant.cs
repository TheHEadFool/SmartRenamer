using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants

// Begin namespace
{
    /// <summary>
    /// =========================================================================
    /// E_RepairConsultant
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Reviews repair opportunities discovered by the Repair Block.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Interpret RepairReport.
    /// • Produce ExpertFindings.
    /// • Never modify ebook files.
    ///
    /// This Consultant does NOT
    /// -------------------------------------------------------------------------
    /// • Repair metadata.
    /// • Read EPUB files.
    /// • Communicate with Scout.
    ///
    /// Those responsibilities belong to the Block and Investigation.
    /// =========================================================================
    /// </summary>
    internal sealed class E_RepairConsultant

    // Begin E_RepairConsultant
    {
        public List<ExpertFinding> Review(
            RepairReport report)

        // Begin Review()
        {
            List<ExpertFinding> findings = new();

            if (report.RepairableBooks > 0)
            {
                findings.Add(
                    new ExpertFinding
                    {
                        FoundSomething = true,
                        Summary =
                            $"{report.RepairableBooks} ebooks contain metadata that may be improved."
                    });
            }

            if (report.MissingTitles > 0)
            {
                findings.Add(
                    new ExpertFinding
                    {
                        FoundSomething = true,
                        Summary =
                            $"{report.MissingTitles} ebooks are missing titles."
                    });
            }

            if (report.MissingAuthors > 0)
            {
                findings.Add(
                    new ExpertFinding
                    {
                        FoundSomething = true,
                        Summary =
                            $"{report.MissingAuthors} ebooks are missing authors."
                    });
            }

            if (report.MissingIsbns > 0)
            {
                findings.Add(
                    new ExpertFinding
                    {
                        FoundSomething = true,
                        Summary =
                            $"{report.MissingIsbns} ebooks are missing ISBNs."
                    });
            }

            if (report.MissingCovers > 0)
            {
                findings.Add(
                    new ExpertFinding
                    {
                        FoundSomething = true,
                        Summary =
                            $"{report.MissingCovers} ebooks are missing cover images."
                    });
            }

            return findings;

        } // End Review()

    } // End E_RepairConsultant

} // End namespace