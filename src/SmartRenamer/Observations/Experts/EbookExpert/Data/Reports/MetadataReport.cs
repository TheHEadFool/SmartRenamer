using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Data.Reports
{
    /// <summary>
    /// Represents everything the Metadata Block learned about
    /// the ebook collection.
    /// </summary>
    public class MetadataReport
    {
        //-----------------------------------------------------
        // Collection Statistics
        //-----------------------------------------------------

        public int TotalFiles { get; set; }

        public int EpubFiles { get; set; }

        //-----------------------------------------------------
        // Metadata Availability
        //-----------------------------------------------------

        public int Titles { get; set; }

        public int Authors { get; set; }

        public int Series { get; set; }

        public int Publishers { get; set; }

        public int Languages { get; set; }

        public int Isbns { get; set; }

        public int PublicationDates { get; set; }

        public int Descriptions { get; set; }

        public int Covers { get; set; }

        public int Subjects { get; set; }

        public int RightsStatements { get; set; }

        //-----------------------------------------------------
        // Missing Metadata
        //-----------------------------------------------------

        public int MissingTitles { get; set; }

        public int MissingAuthors { get; set; }

        public int MissingSeries { get; set; }

        public int MissingPublishers { get; set; }

        public int MissingLanguages { get; set; }

        public int MissingIsbns { get; set; }

        public int MissingPublicationDates { get; set; }

        public int MissingDescriptions { get; set; }

        public int MissingCovers { get; set; }

        //-----------------------------------------------------
        // Consistency
        //-----------------------------------------------------

        public int DuplicateIsbns { get; set; }

        public int DuplicateTitles { get; set; }

        public int ConflictingAuthors { get; set; }

        public int ConflictingSeries { get; set; }

        //-----------------------------------------------------
        // Quality
        //-----------------------------------------------------

        public int CompleteMetadata { get; set; }

        public int IncompleteMetadata { get; set; }

        public int ExcellentMetadata { get; set; }

        public int NeedsAttention { get; set; }

        //-----------------------------------------------------
        // Findings
        //-----------------------------------------------------

        public List<MetadataFinding> Findings { get; } = new();

        public List<MetadataEvidence> Evidence { get; } = new();
    }
}