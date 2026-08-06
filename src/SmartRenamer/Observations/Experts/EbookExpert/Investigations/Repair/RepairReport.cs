using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair

// Begin namespace
{
    /// <summary>
    /// =========================================================================
    /// RepairReport
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Contains factual information describing repair opportunities
    /// discovered within an ebook collection.
    ///
    /// Reports contain facts.
    /// Reports never make recommendations.
    /// =========================================================================
    /// </summary>
    public sealed class RepairReport

    // Begin RepairReport
    {
        //---------------------------------------------------------
        // Repair Opportunities
        //---------------------------------------------------------

        public int MissingTitles { get; set; }

        public int MissingAuthors { get; set; }

        public int MissingIsbns { get; set; }

        public int MissingPublishers { get; set; }

        public int MissingLanguages { get; set; }

        public int MissingDescriptions { get; set; }

        public int MissingCovers { get; set; }

        //---------------------------------------------------------
        // Collection Statistics
        //---------------------------------------------------------

        public int RepairableBooks { get; set; }

        //---------------------------------------------------------
        // Evidence
        //---------------------------------------------------------

        public List<string> MissingTitleBooks { get; } = new();

        public List<string> MissingAuthorBooks { get; } = new();

        public List<string> MissingIsbnBooks { get; } = new();

        public List<string> MissingPublisherBooks { get; } = new();

        public List<string> MissingLanguageBooks { get; } = new();

        public List<string> MissingDescriptionBooks { get; } = new();

        public List<string> MissingCoverBooks { get; } = new();

    } // End RepairReport

} // End namespace
