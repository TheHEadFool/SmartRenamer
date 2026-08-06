using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Contents

// Begin namespace
{
    /// <summary>
    /// =========================================================================
    /// ContentsReport
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Contains factual information describing the contents of an ebook.
    ///
    /// Reports contain facts.
    /// Reports never make recommendations.
    /// =========================================================================
    /// </summary>
    public sealed class ContentsReport

    // Begin ContentsReport
    {
        //---------------------------------------------------------
        // Navigation
        //---------------------------------------------------------

        public int BooksWithTableOfContents { get; set; }

        public int BooksWithoutTableOfContents { get; set; }

        //---------------------------------------------------------
        // Structure
        //---------------------------------------------------------

        public int TotalBooks { get; set; }

        public int TotalChapters { get; set; }

        public int BooksWithIllustrations { get; set; }

        public int BooksWithEmbeddedFonts { get; set; }

        //---------------------------------------------------------
        // Evidence
        //---------------------------------------------------------

        public List<string> MissingTableOfContents { get; } = new();

        public List<string> BrokenNavigation { get; } = new();

        public List<string> Notes { get; } = new();

    } // End ContentsReport

} // End namespace