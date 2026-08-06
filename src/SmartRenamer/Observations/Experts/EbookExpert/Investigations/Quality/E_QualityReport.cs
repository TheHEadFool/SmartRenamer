using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Quality
{
    /// <summary>
    /// ================================================================
    /// QualityReport
    /// ================================================================
    ///
    /// Preserves everything discovered while researching the
    /// overall health of an ebook collection.
    ///
    /// This class contains facts only.
    ///
    /// It does not interpret those facts.
    /// ================================================================
    /// </summary>
    public class QualityReport
    {
        //-----------------------------------------------------
        // Metadata Health
        //-----------------------------------------------------

        public int ExcellentMetadata { get; set; }

        public int NeedsAttention { get; set; }

        //-----------------------------------------------------
        // Covers
        //-----------------------------------------------------

        public int MissingCovers { get; set; }

        //-----------------------------------------------------
        // File Integrity
        //-----------------------------------------------------

        public int DamagedBooks { get; set; }

        //-----------------------------------------------------
        // Evidence
        //-----------------------------------------------------

        public List<string> Evidence { get; } = new();
    }
}