using System.Collections.Generic;
using Scout.Observations.Experts.EbookExpert.Investigations.Organization;

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
    /// • Preserve which organization dimensions are actually available.
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
        // Available Organization Dimensions
        //-----------------------------------------------------

        /// <summary>
        /// Organization dimensions for which this collection
        /// contains usable metadata.
        ///
        /// This describes what Scout knows, not what the user
        /// has chosen to do with it.
        /// </summary>
        public List<OrganizationDimension> AvailableDimensions { get; } = new();

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