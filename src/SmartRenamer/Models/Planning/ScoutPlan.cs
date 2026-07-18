using System.Collections.Generic;

namespace SmartRenamer.Models.Planning
{
    /// <summary>
    /// Represents Scout's complete proposed plan before
    /// any files are modified.
    /// </summary>
    public class ScoutPlan
    {
        /// <summary>
        /// What the user is trying to accomplish.
        /// </summary>
        public string Goal { get; set; } = "";

        /// <summary>
        /// Leave the originals untouched while creating
        /// a newly organized copy.
        /// </summary>
        public bool ProtectOriginals { get; set; } = true;

        /// <summary>
        /// Should Scout rename files?
        /// </summary>
        public bool RenameFiles { get; set; }

        /// <summary>
        /// Should Scout organize into folders?
        /// </summary>
        public bool OrganizeFolders { get; set; } = true;

        /// <summary>
        /// Destination folder for the completed project.
        /// </summary>
        public string DestinationFolder { get; set; } = "";

        /// <summary>
        /// Human-readable explanation of the organization strategy.
        /// Example:
        /// "Group photos by Year / Month"
        /// </summary>
        public string OrganizationStrategy { get; set; } = "";

        /// <summary>
        /// Optional text placed before each filename.
        /// Example:
        /// "Vacation 2026 -"
        /// </summary>
        public string FilenamePrefix { get; set; } = "";

        /// <summary>
        /// Optional text placed after each filename.
        /// Example:
        /// "- Edited"
        /// </summary>
        public string FilenameSuffix { get; set; } = "";

        /// <summary>
        /// True if the original filename should be preserved.
        /// </summary>
        public bool KeepOriginalFilename { get; set; } = true;
        /// <summary>
        /// Human-readable explanation of the naming strategy.
        /// Example:
        /// "Readable names with dates"
        /// </summary>
        public string NamingStrategy { get; set; } = "";

        /// <summary>
        /// Files that Scout intends to rename.
        /// </summary>
        public List<Rename.RenamePreview> RenamePreview { get; } = new();

        /// <summary>
        /// Summary values shown to the user before execution.
        /// </summary>
        public int FilesToProcess { get; set; }

        public int FoldersToCreate { get; set; }

        public int FilesToRename { get; set; }

        public int FilesToMove { get; set; }

        public int FilesToCopy { get; set; }

        public int FilesToDelete { get; set; }

        /// <summary>
        /// Messages explaining why Scout built this plan.
        /// These are shown to the user in plain language.
        /// </summary>
        public List<string> Reasoning { get; } = new();

        /// <summary>
        /// Recommendations discovered during planning.
        /// </summary>
        public List<string> Recommendations { get; } = new();
    }
}