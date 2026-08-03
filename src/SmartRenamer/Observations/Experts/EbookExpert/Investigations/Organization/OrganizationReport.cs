using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization
{
    /// <summary>
    /// =========================================================================
    /// OrganizationReport
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Preserves everything discovered while researching how an ebook
    /// collection is organized.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Preserve organization statistics.
    /// • Preserve organization evidence.
    /// • Remain completely neutral.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Interpret results.
    /// • Produce recommendations.
    /// • Communicate with Scout.
    ///
    /// Those responsibilities belong to the Consultant.
    /// =========================================================================
    /// </summary>
    public class OrganizationReport
    {
        //-----------------------------------------------------
        // Series
        //-----------------------------------------------------

        public int BooksInSeries { get; set; }

        public int BooksWithoutSeries { get; set; }

        public int SeriesCount { get; set; }

        public int LargestSeriesSize { get; set; }

        public int SingleBookSeries { get; set; }

        //-----------------------------------------------------
        // Publishers
        //-----------------------------------------------------

        public int PublisherCount { get; set; }

        //-----------------------------------------------------
        // Languages
        //-----------------------------------------------------

        public int LanguageCount { get; set; }

        //-----------------------------------------------------
        // Evidence
        //-----------------------------------------------------

        public List<OrganizationEvidence> Evidence { get; } = new();
    }
}