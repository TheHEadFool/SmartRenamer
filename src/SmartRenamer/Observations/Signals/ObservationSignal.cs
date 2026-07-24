namespace SmartRenamer.Observations.Signals
{
    /// <summary>
    /// =========================================================================
    /// ObservationSignal
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Speak a common language across every Expert."
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// ObservationSignals are structured facts discovered by Specialists.
    /// They allow Experts to reason about collections without interpreting
    /// human-readable text.
    ///
    /// Why it exists
    /// -------------------------------------------------------------------------
    /// Human-readable summaries are excellent for users but difficult for
    /// software to reason about. ObservationSignals provide a common
    /// vocabulary that every Specialist can produce and every Expert can
    /// understand.
    ///
    /// Relationship to Scout
    /// -------------------------------------------------------------------------
    /// Files
    ///     ↓
    /// Building Blocks
    ///     ↓
    /// Specialists
    ///     ↓
    /// Observation Signals
    ///     ↓
    /// Expert Findings
    ///     ↓
    /// Expert Insights
    ///     ↓
    /// Scout
    /// =========================================================================
    /// </summary>
    public enum ObservationSignal
    {
        Unknown,

        // Music Collection
        MusicDetected,
        SingleAlbum,
        MultipleAlbums,

        // Artwork
        AlbumArtworkFound,
        AlbumArtworkMissing,

        // Metadata
        MetadataPresent,
        MissingArtistMetadata,
        MissingAlbumMetadata,
        MissingTitleMetadata,
        MissingTrackMetadata
    }
}