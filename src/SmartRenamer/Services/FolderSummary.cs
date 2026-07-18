using System.Collections.Generic;
using SmartRenamer.Models;

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
        /// Legacy file list used by existing rename code.
        /// </summary>
        public List<string> Files { get; } = new();

        /// <summary>
        /// Rich file information used by Scout.
        /// </summary>
        public List<FileContext> FileContexts { get; } = new();

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