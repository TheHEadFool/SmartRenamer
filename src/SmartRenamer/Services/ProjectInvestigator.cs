using SmartRenamer.Models;

namespace SmartRenamer.Services
{
    public class ProjectInvestigator
    {
        private readonly FolderPicker folderPicker = new();

        private readonly FolderScanner folderScanner = new();

        public ProjectContext? Investigate()
        {
            string? folder = folderPicker.PickFolder();

            if (string.IsNullOrWhiteSpace(folder))
                return null;

            FolderSummary summary = folderScanner.Scan(folder);

            return new ProjectContext
            {
                Folder = summary
            };
        }
    }
}