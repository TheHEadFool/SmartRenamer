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
    /// • Investigate missing cover images.
    ///
    /// Those responsibilities belong to future enrichment services,
    /// the Block, the Investigation, and the dedicated Cover Investigation.
    ///
    /// Architecture
    /// -------------------------------------------------------------------------
    /// Metadata Investigation
    ///         ↓
    /// MetadataReport
    ///         ↓
    /// E_EnrichmentInvestigation
    ///         ↓
    /// EnrichmentBlock
    ///         ↓
    /// EnrichmentReport
    ///         ↓
    /// E_EnrichmentConsultant
    ///         ↓
    /// ExpertFindings
    ///
    /// Cover-related findings are intentionally handled by
    /// E_CoverInvestigation and E_CoverConsultant so that each domain
    /// condition has one clear owner within the Ebook Expert.
    ///
    /// Design Principle
    /// -------------------------------------------------------------------------
    /// The Consultant interprets enrichment facts discovered by the Block.
    /// It should identify meaningful enrichment opportunities without
    /// duplicating findings that belong to another Investigation.
    /// =========================================================================
    /// </summary>
    internal sealed class E_EnrichmentConsultant

    // Begin E_EnrichmentConsultant
    {
        /// <summary>
        /// Reviews enrichment opportunities discovered by the Enrichment Block.
        /// </summary>
        /// <param name="report">
        /// The EnrichmentReport produced by the Enrichment Block.
        /// </param>
        /// <returns>
        /// A list of ExpertFindings describing enrichment opportunities.
        /// </returns>
        public List<ExpertFinding> Review(
            EnrichmentReport report)

        // Begin Review()
        {
            List<ExpertFinding> findings = new();

            //---------------------------------------------------------
            // General enrichment opportunity
            //---------------------------------------------------------
            // Identifies ebooks for which additional metadata may be
            // available or useful.
            //---------------------------------------------------------

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

            //---------------------------------------------------------
            // Missing series information
            //---------------------------------------------------------
            // Series information is an enrichment concern and remains
            // owned by this Consultant.
            //---------------------------------------------------------

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

            //---------------------------------------------------------
            // Missing descriptions
            //---------------------------------------------------------
            // Descriptions are an enrichment concern and remain owned
            // by this Consultant.
            //---------------------------------------------------------

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

            //---------------------------------------------------------
            // Missing cover images
            //---------------------------------------------------------
            // Cover findings are intentionally NOT produced here.
            //
            // E_CoverInvestigation and E_CoverConsultant now own the
            // cover domain. Keeping that responsibility in one place
            // prevents duplicate ExpertFindings and duplicate
            // recommendations.
            //---------------------------------------------------------

            return findings;

        } // End Review()

    } // End E_EnrichmentConsultant

} // End namespace