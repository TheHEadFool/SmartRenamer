namespace SmartRenamer.Models.Rename
{
    public class RenamePreview
    {
        public string FullPath { get; set; } = "";

        public string CurrentName { get; set; } = "";

        public string NewName { get; set; } = "";

        public bool WillRename =>
            CurrentName != NewName;
    }
}