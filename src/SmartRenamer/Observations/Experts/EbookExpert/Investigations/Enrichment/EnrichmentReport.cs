using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Enrichment

// Begin namespace
{
    /// <summary>
    /// =========================================================================
    /// EnrichmentReport
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Contains factual information describing opportunities to enrich
    /// an ebook collection.
    ///
    /// Reports contain facts.
    /// Reports never make recommendations.
    /// =========================================================================
    /// </summary>
    public sealed class EnrichmentReport

    // Begin EnrichmentReport
    {
        //---------------------------------------------------------
        // Metadata Opportunities
        //---------------------------------------------------------

        public int MissingSeries { get; set; }

        public int MissingDescriptions { get; set; }

        public int MissingGenres { get; set; }

        public int MissingTags { get; set; }

        public int MissingPublicationDates { get; set; }

        public int MissingCoverImages { get; set; }

        //---------------------------------------------------------
        // Collection Opportunities
        //---------------------------------------------------------

        public int IncompleteSeries { get; set; }

        public int AuthorsWithAdditionalBooks { get; set; }

        public int BooksEligibleForEnrichment { get; set; }

        //---------------------------------------------------------
        // Evidence
        //---------------------------------------------------------

        public List<string> MissingSeriesBooks { get; } = new();

        public List<string> MissingDescriptionBooks { get; } = new();

        public List<string> MissingGenreBooks { get; } = new();

        public List<string> MissingTagBooks { get; } = new();

        public List<string> Notes { get; } = new();

    } // End EnrichmentReport

} // End namespace