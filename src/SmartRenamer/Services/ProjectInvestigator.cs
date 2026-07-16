using SmartRenamer.Models;

namespace SmartRenamer.Services
{
    public class ProjectInvestigator
    {
        private readonly FolderPicker folderPicker = new();

        private readonly FolderScanner folderScanner = new();

        private readonly ProjectAnalyzer projectAnalyzer = new();

        public ProjectContext? Investigate()
        {
            string? folder = folderPicker.PickFolder();

            if (string.IsNullOrWhiteSpace(folder))
                return null;

            FolderSummary summary = folderScanner.Scan(folder);

            ProjectContext context = new()
            {
                Folder = summary
            };

            // Let the analyzer determine the project type,
            // observations, and recommended capabilities.
            projectAnalyzer.Analyze(context);

            return context;
        }
    }
}