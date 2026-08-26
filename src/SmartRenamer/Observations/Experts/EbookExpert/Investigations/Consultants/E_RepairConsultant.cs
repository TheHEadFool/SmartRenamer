using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants
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
    /// • Identify when additional research may be appropriate.
    /// • Never modify ebook files.
    ///
    /// This Consultant does NOT
    /// -------------------------------------------------------------------------
    /// • Repair metadata.
    /// • Read EPUB files.
    /// • Perform external research.
    /// • Communicate directly with Scout.
    ///
    /// Those responsibilities belong to the appropriate Ebook Expert
    /// Investigation or later repair capability.
    /// =========================================================================
    /// </summary>
    internal sealed class E_RepairConsultant
    {
        public List<ExpertFinding> Review(
            RepairReport report)
        {
            List<ExpertFinding> findings = new();

            //---------------------------------------------------------
            // Missing ISBN
            //---------------------------------------------------------

            if (report.MissingIsbns > 0)
            {
                ExpertFinding finding = new()
                {
                    FoundSomething = true,
                    Summary =
                        $"{report.MissingIsbns} ebooks are missing ISBN information.",
                    Confidence = 1.0
                };

                finding.Evidence.Add(
                    $"Ebooks missing ISBN information: {report.MissingIsbns}");

                foreach (RepairOpportunity opportunity in report.Opportunities)
                {
                    if (!opportunity.MissingIsbn)
                        continue;

                    string fileName =
                        opportunity.Record.File.CurrentName;

                    finding.Evidence.Add(
                        $"ISBN missing: {fileName}");
                }

                finding.Questions.Add(
                    "Would you like Scout to research the missing ISBN information?");

                findings.Add(finding);
            }

            //---------------------------------------------------------
            // Other repair opportunities
            //
            // These remain factual findings for now. They will be
            // given their own actionable research/repair behavior
            // after the first ISBN repair path is proven.
            //---------------------------------------------------------

            if (report.MissingTitles > 0)
            {
                findings.Add(
                    new ExpertFinding
                    {
                        FoundSomething = true,
                        Summary =
                            $"{report.MissingTitles} ebooks are missing titles.",
                        Confidence = 1.0
                    });
            }

            if (report.MissingAuthors > 0)
            {
                findings.Add(
                    new ExpertFinding
                    {
                        FoundSomething = true,
                        Summary =
                            $"{report.MissingAuthors} ebooks are missing authors.",
                        Confidence = 1.0
                    });
            }

            if (report.MissingPublishers > 0)
            {
                findings.Add(
                    new ExpertFinding
                    {
                        FoundSomething = true,
                        Summary =
                            $"{report.MissingPublishers} ebooks are missing publishers.",
                        Confidence = 1.0
                    });
            }

            if (report.MissingLanguages > 0)
            {
                findings.Add(
                    new ExpertFinding
                    {
                        FoundSomething = true,
                        Summary =
                            $"{report.MissingLanguages} ebooks are missing languages.",
                        Confidence = 1.0
                    });
            }

            if (report.MissingDescriptions > 0)
            {
                findings.Add(
                    new ExpertFinding
                    {
                        FoundSomething = true,
                        Summary =
                            $"{report.MissingDescriptions} ebooks are missing descriptions.",
                        Confidence = 1.0
                    });
            }

            if (report.MissingCovers > 0)
            {
                findings.Add(
                    new ExpertFinding
                    {
                        FoundSomething = true,
                        Summary =
                            $"{report.MissingCovers} ebooks are missing cover images.",
                        Confidence = 1.0
                    });
            }

            return findings;
        }
    }
}