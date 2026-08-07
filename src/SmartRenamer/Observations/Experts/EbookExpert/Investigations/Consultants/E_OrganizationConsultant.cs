using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization;

// Future:
//
// Consultants classify findings as:
//
// Observation
// Opportunity
// Warning
//
// The Conversation Planner will use these classifications
// to determine which findings should become conversations.

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants
{
    internal sealed class E_OrganizationConsultant
    {
        public List<ExpertFinding> Review(
            OrganizationReport report)
        {
            List<ExpertFinding> findings = new();

            if (report.BooksWithoutSeries > 0)
            {
                findings.Add(new ExpertFinding
                {
                    FoundSomething = true,
                    Summary =
                        $"Add series information to {report.BooksWithoutSeries} books."
                });
            }

            if (report.SingleBookSeries > 10)
            {
                findings.Add(new ExpertFinding
                {
                    FoundSomething = true,
                    Summary =
                        $"Review {report.SingleBookSeries} single-book series for naming consistency."
                });
            }

            if (report.SeriesCount > 0)
            {
                findings.Add(new ExpertFinding
                {
                    FoundSomething = true,
                    Summary =
                        $"Library contains {report.SeriesCount} series."
                });
            }

            if (report.PublisherCount > 0)
            {
                findings.Add(new ExpertFinding
                {
                    FoundSomething = true,
                    Summary =
                        $"Library contains books from {report.PublisherCount} publishers."
                });
            }

            if (report.LanguageCount > 0)
            {
                findings.Add(new ExpertFinding
                {
                    FoundSomething = true,
                    Summary =
                        $"Library contains books in {report.LanguageCount} languages."
                });
            }

            return findings;
        }
    }
}