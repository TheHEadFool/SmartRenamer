using System;
using TagLib;
using SmartRenamer.Models;

namespace SmartRenamer.Observations.BuildingBlocks
{
    /// <summary>
    /// =========================================================================
    /// MusicMetadataReader
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Read metadata. Do not interpret it."
    ///
    /// Domain
    /// -------------------------------------------------------------------------
    /// Music Metadata
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Provides a single location for reading metadata from music files.
    /// This class isolates SmartRenamer from the TagLib# library.
    ///
    /// Why it exists
    /// -------------------------------------------------------------------------
    /// Specialists should investigate their domain, not communicate directly
    /// with external libraries. By placing all TagLib# interaction here,
    /// SmartRenamer remains independent of the underlying metadata library.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Open music files
    /// • Read metadata
    /// • Return TagLib information
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Analyze metadata
    /// • Report observations
    /// • Make recommendations
    /// • Modify files
    ///
    /// Relationship to Scout
    /// -------------------------------------------------------------------------
    /// Scout
    ///   └── MusicExpert
    ///         └── MetadataSpecialist
    ///               └── MusicMetadataReader
    ///                     └── TagLib#
    /// =========================================================================
    /// </summary>
    public static class MusicMetadataReader
    {
        public static Tag Read(FileContext file)
        {
            try
            {
                var musicFile = TagLib.File.Create(file.OriginalFullPath);
                return musicFile.Tag;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}