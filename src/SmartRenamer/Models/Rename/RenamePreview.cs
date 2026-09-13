namespace SmartRenamer.Models.Rename
{
    public class RenamePreview
    {
        /// <summary>
        /// Stable source identity for the file represented by this preview.
        ///
        /// This is deliberately separate from FullPath because FullPath may
        /// point to Scout's protected working copy.
        /// </summary>
        public string OriginalFullPath { get; set; } = "";

        public string FullPath { get; set; } = "";

        public string CurrentName { get; set; } = "";

        public string NewName { get; set; } = "";

        // New: destination folder for organization.
        public string DestinationFolder { get; set; } = "";

        // New: full destination path.
        public string DestinationPath { get; set; } = "";

        public bool WillRename =>
            CurrentName != NewName;

        public bool WillMove =>
            !string.IsNullOrWhiteSpace(DestinationFolder);

        public bool HasChanges =>
            WillRename || WillMove;
    }
}