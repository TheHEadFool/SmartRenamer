using System.Collections.Generic;

namespace SmartRenamer.Services
{
    /// <summary>
    /// Summarizes the contents of a folder after investigation.
    /// </summary>
    public class FolderSummary
    {
        /// <summary>
        /// Root folder selected by the user.
        /// </summary>
        public string FolderPath { get; set; } = "";

        public int FolderCount { get; set; }

        public int FileCount { get; set; }

        public int ImageCount { get; set; }

        public int VideoCount { get; set; }

        public int DocumentCount { get; set; }

        public long TotalBytes { get; set; }

        public string OldestFileDate { get; set; } = "";

        public string NewestFileDate { get; set; } = "";

        //----------------------------------------------------
        // File Details
        //----------------------------------------------------

        /// <summary>
        /// Every file discovered during investigation.
        /// This allows the planner to produce previews
        /// without scanning the folder again.
        /// </summary>
        public List<string> Files { get; } = new();

        /// <summary>
        /// Unique file extensions discovered.
        /// </summary>
        public List<string> Extensions { get; } = new();

        //----------------------------------------------------
        // Project Characteristics
        //----------------------------------------------------

        public bool HasRawPhotos { get; set; }

        public bool HasExifImages { get; set; }

        public bool HasVideos { get; set; }

        public bool HasSubfolders { get; set; }
    }
}