using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Duplicates
{
    /// <summary>
    /// =========================================================================
    /// E_DuplicateReport
    /// =========================================================================
    ///
    /// Contains factual information describing duplicate relationships
    /// discovered in an ebook library.
    ///
    /// Reports contain facts.
    /// They never make recommendations.
    /// =========================================================================
    /// </summary>
    public class E_DuplicateReport
    {
        //---------------------------------------------------------
        // Duplicate Counts
        //---------------------------------------------------------

        public int DuplicateTitles { get; set; }

        public int DuplicateIsbns { get; set; }

        public int DuplicateFileNames { get; set; }

        public int MultipleEditions { get; set; }

        public List<string> MultipleEditionList { get; } = new();

        //---------------------------------------------------------
        // Collection Statistics
        //---------------------------------------------------------

        public int DuplicateGroups { get; set; }

        public int DuplicateBooks { get; set; }

        //---------------------------------------------------------
        // Evidence
        //---------------------------------------------------------

        public List<string> DuplicateTitleList { get; } = new();

        public List<string> DuplicateIsbnList { get; } = new();

        public List<string> DuplicateFileNameList { get; } = new();
    }
}