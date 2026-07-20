using System.Collections.Generic;

namespace SmartRenamer.Models.Rename
{
    /// <summary>
    /// Result of a rename operation.
    /// </summary>
    public class RenameResult
    {
        /// <summary>
        /// True when at least one file was successfully renamed.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Number of files successfully renamed.
        /// </summary>
        public int FilesRenamed { get; set; }

        /// <summary>
        /// Number of files skipped.
        /// </summary>
        public int FilesSkipped { get; set; }

        /// <summary>
        /// Full path to the organized folder created by Scout.
        /// Null if the operation failed.
        /// </summary>
        public string? OutputFolder { get; set; }

        /// <summary>
        /// Records every successful rename.
        /// Used later to support Undo.
        /// </summary>
        public RenameTransaction Transaction { get; } = new();

        /// <summary>
        /// Errors that prevented a file from being renamed.
        /// </summary>
        public List<string> Errors { get; } = new();

        /// <summary>
        /// Informational warnings (non-fatal).
        /// </summary>
        public List<string> Warnings { get; } = new();
    }
}