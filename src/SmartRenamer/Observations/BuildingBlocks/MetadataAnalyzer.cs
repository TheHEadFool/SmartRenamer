using System;

namespace SmartRenamer.Observations.BuildingBlocks
{
    /// <summary>
    /// =========================================================================
    /// MetadataAnalyzer
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Measure first. Interpret later."
    ///
    /// Domain
    /// -------------------------------------------------------------------------
    /// Music Metadata
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Accumulates statistics about music metadata. This class performs
    /// counting only. It does not read files, interpret results, or make
    /// recommendations.
    ///
    /// Why it exists
    /// -------------------------------------------------------------------------
    /// MetadataSpecialist should focus on observations, not bookkeeping.
    /// This class centralizes all counting logic.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Count readable music files
    /// • Count metadata fields
    /// • Provide summary statistics
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Read music files
    /// • Use TagLib directly
    /// • Report observations
    /// • Recommend actions
    ///
    /// Relationship to Scout
    /// -------------------------------------------------------------------------
    /// Scout
    ///   └── MusicExpert
    ///         └── MetadataSpecialist
    ///               └── MetadataAnalyzer
    /// =========================================================================
    /// </summary>
    public class MetadataAnalyzer
    {
        public int MusicFiles { get; private set; }

        public int ArtistCount { get; private set; }

        public int AlbumCount { get; private set; }

        public int TitleCount { get; private set; }

        public int TrackCount { get; private set; }

        public void CountFile()
        {
            MusicFiles++;
        }

        public void CountArtist()
        {
            ArtistCount++;
        }

        public void CountAlbum()
        {
            AlbumCount++;
        }

        public void CountTitle()
        {
            TitleCount++;
        }

        public void CountTrack()
        {
            TrackCount++;
        }
    }
}