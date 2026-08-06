using SmartRenamer.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Models;
using SmartRenamer.Observations.Experts.EbookExpert.Data.Reports;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Enrichment

// Begin namespace
{
    /// <summary>
    /// =========================================================================
    /// EnrichmentBlock
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Collects factual information describing opportunities to enrich
    /// an ebook collection.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Examine existing ebook metadata.
    /// • Identify missing enrichment information.
    /// • Identify books that may benefit from additional information.
    /// • Produce an EnrichmentReport.
    ///
    /// This Block does NOT
    /// -------------------------------------------------------------------------
    /// • Search online services.
    /// • Download metadata.
    /// • Modify ebook files.
    /// • Make recommendations.
    /// • Communicate with Scout.
    ///
    /// Those responsibilities belong to later enrichment services,
    /// Consultants, and the Investigation.
    /// =========================================================================
    /// </summary>
    internal sealed class EnrichmentBlock

    // Begin EnrichmentBlock
    {
        public EnrichmentReport Analyze(
            MetadataReport metadataReport)

        // Begin Analyze()
        {
            EnrichmentReport report = new();

            //---------------------------------------------------------
            // Examine every metadata record.
            //---------------------------------------------------------

            foreach (MetadataRecord record in metadataReport.Records)

            // Begin foreach MetadataRecord
            {
                E_EbookMetadata metadata =
                    record.Metadata;

                bool needsEnrichment = false;

                //-----------------------------------------------------
                // Series
                //-----------------------------------------------------

                if (string.IsNullOrWhiteSpace(metadata.Series))
                {
                    report.MissingSeries++;

                    report.MissingSeriesBooks.Add(
                        record.File.CurrentName);

                    needsEnrichment = true;
                }

                //-----------------------------------------------------
                // Description
                //-----------------------------------------------------

                if (string.IsNullOrWhiteSpace(metadata.Description))
                {
                    report.MissingDescriptions++;

                    report.MissingDescriptionBooks.Add(
                        record.File.CurrentName);

                    needsEnrichment = true;
                }

                //-----------------------------------------------------
                // Cover
                //-----------------------------------------------------

                if (!metadata.HasCover)
                {
                    report.MissingCoverImages++;

                    needsEnrichment = true;
                }

                //-----------------------------------------------------
                // Publisher / publication information
                //-----------------------------------------------------

                if (string.IsNullOrWhiteSpace(metadata.Publisher))
                {
                    needsEnrichment = true;
                }

                //-----------------------------------------------------
                // Language
                //-----------------------------------------------------

                if (string.IsNullOrWhiteSpace(metadata.Language))
                {
                    needsEnrichment = true;
                }

                //-----------------------------------------------------
                // Count this book once, regardless of how many
                // enrichment opportunities were discovered.
                //-----------------------------------------------------

                if (needsEnrichment)
                {
                    report.BooksEligibleForEnrichment++;
                }

            } // End foreach MetadataRecord

            //---------------------------------------------------------
            // Future Enrichment
            //---------------------------------------------------------
            //
            // These require knowledge beyond the local EPUB metadata
            // and will be populated by future enrichment services:
            //
            // • Missing genres
            // • Missing tags
            // • Missing publication dates
            // • Incomplete series
            // • Additional books by known authors
            //
            // The Report already has room for these facts so future
            // versions can grow without redesigning the Investigation.
            //---------------------------------------------------------

            return report;

        } // End Analyze()

    } // End EnrichmentBlock

} // End namespace