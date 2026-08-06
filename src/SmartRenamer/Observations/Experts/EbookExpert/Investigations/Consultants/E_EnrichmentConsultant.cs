using System.Collections.Generic;
using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Investigations.Enrichment;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Consultants

// Begin namespace
{
    /// <summary>
    /// =========================================================================
    /// E_EnrichmentConsultant
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Reviews enrichment opportunities discovered by the Enrichment Block.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Interpret an EnrichmentReport.
    /// • Produce ExpertFindings.
    /// • Never modify ebook files.
    ///
    /// This Consultant does NOT
    /// -------------------------------------------------------------------------
    /// • Download metadata.
    /// • Search online services.
    /// • Modify ebook files.
    /// • Communicate with Scout.
    ///
    /// Those responsibilities belong to future enrichment services,
    /// the Block, and the Investigation.
    /// =========================================================================
    /// </summary>
    internal sealed class E_EnrichmentConsultant

    // Begin E_EnrichmentConsultant
    {
        public List<ExpertFinding> Review(
            EnrichmentReport report)

        // Begin Review()
        {
            List<ExpertFinding> findings = new();

            if (report.BooksEligibleForEnrichment > 0)
            {
                findings.Add(
                    new ExpertFinding
                    {
                        FoundSomething = true,
                        Summary =
                            $"{report.BooksEligibleForEnrichment} ebooks could be enriched with additional metadata."
                    });
            }

            if (report.MissingSeries > 0)
            {
                findings.Add(
                    new ExpertFinding
                    {
                        FoundSomething = true,
                        Summary =
                            $"{report.MissingSeries} ebooks are missing series information."
                    });
            }

            if (report.MissingDescriptions > 0)
            {
                findings.Add(
                    new ExpertFinding
                    {
                        FoundSomething = true,
                        Summary =
                            $"{report.MissingDescriptions} ebooks are missing descriptions."
                    });
            }

            if (report.MissingCoverImages > 0)
            {
                findings.Add(
                    new ExpertFinding
                    {
                        FoundSomething = true,
                        Summary =
                            $"{report.MissingCoverImages} ebooks are missing cover images."
                    });
            }

            return findings;

        } // End Review()

    } // End E_EnrichmentConsultant

} // End namespace