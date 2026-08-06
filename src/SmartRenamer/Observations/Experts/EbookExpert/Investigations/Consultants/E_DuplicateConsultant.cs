using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Duplicates;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants
{
    /// <summary>
    /// =========================================================================
    /// E_DuplicateConsultant
    /// =========================================================================
    ///
    /// Reviews the Duplicate Report and determines which findings
    /// the Ebook Expert should report to Scout.
    ///
    /// Consultants interpret facts.
    /// They never collect them.
    /// =========================================================================
    /// </summary>
    internal sealed class E_DuplicateConsultant
    {
        public List<ExpertFinding> Review(
            E_DuplicateReport report)
        {
            List<ExpertFinding> findings = new();

            if (report.DuplicateTitles > 0)
            {
                findings.Add(new ExpertFinding
                {
                    FoundSomething = true,
                    Summary =
                        $"{report.DuplicateTitles} duplicate titles were found."
                });
            }

            if (report.DuplicateIsbns > 0)
            {
                findings.Add(new ExpertFinding
                {
                    FoundSomething = true,
                    Summary =
                        $"{report.DuplicateIsbns} duplicate ISBNs were found."
                });
            }

            if (report.DuplicateFileNames > 0)
            {
                findings.Add(new ExpertFinding
                {
                    FoundSomething = true,
                    Summary =
                        $"{report.DuplicateFileNames} duplicate filenames were found."
                });
            }

            if (report.MultipleEditions > 0)
            {
                findings.Add(new ExpertFinding
                {
                    FoundSomething = true,
                    Summary =
                        $"{report.MultipleEditions} books have multiple editions."
                });
            }

            return findings;
        }
    }
}