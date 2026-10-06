using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Quality;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants
{
    internal sealed class E_QualityConsultant
    {
        public List<ExpertFinding> Review(
            QualityReport report)
        {
            List<ExpertFinding> findings = new();

            if (report.NeedsAttention > 0)
            {
                findings.Add(new ExpertFinding
                {
                    FoundSomething = true,
                    Summary =
                        $"{EbookGrammar.CountNoun(report.NeedsAttention)} {EbookGrammar.HasHave(report.NeedsAttention)} incomplete metadata."
                });
            }

            if (report.MissingCovers > 0)
            {
                findings.Add(new ExpertFinding
                {
                    FoundSomething = true,
                    Summary =
                        $"{EbookGrammar.CountNoun(report.MissingCovers)} {EbookGrammar.IsAre(report.MissingCovers)} missing cover images."
                });
            }

            return findings;
        }
    }
}