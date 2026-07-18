namespace SmartRenamer.Models.Rename
{
    public class RenamePreview
    {
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