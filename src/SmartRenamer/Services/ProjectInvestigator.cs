using SmartRenamer.Models;
using System.IO;

namespace SmartRenamer.Services
{
    public class ProjectInvestigator
    {
        private readonly FolderPicker folderPicker = new();

        private readonly FolderScanner folderScanner = new();

        private readonly ProjectAnalyzer projectAnalyzer = new();

        /// <summary>
        /// Lets the caller select a folder without beginning investigation.
        ///
        /// This allows the Guide to ask any required user questions
        /// between folder selection and Expert investigation.
        /// </summary>
        public string? PickFolder()
        {
            return folderPicker.PickFolder();
        }

        /// <summary>
        /// Investigates a folder that has already been selected.
        /// </summary>
        public ProjectContext? Investigate(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                return null;

            FolderSummary summary =
                folderScanner.Scan(folder);

            ProjectContext context = new()
            {
                Folder = summary
            };

            string sourceFolderName =
                new DirectoryInfo(folder).Name;

            string? parentFolder =
                Directory.GetParent(folder)?.FullName;

            if (!string.IsNullOrWhiteSpace(sourceFolderName) &&
                !string.IsNullOrWhiteSpace(parentFolder))
            {
                context.Organization.DestinationRoot =
                    Path.Combine(
                        parentFolder,
                        $"{sourceFolderName}_Organized");
            }

            // Let the analyzer determine the project type,
            // observations, and recommended capabilities.
            projectAnalyzer.Analyze(context);

            return context;
        }

        /// <summary>
        /// Existing convenience entry point.
        ///
        /// Preserved so existing callers continue to work exactly
        /// as they did before.
        /// </summary>
        public ProjectContext? Investigate()
        {
            string? folder =
                PickFolder();

            if (string.IsNullOrWhiteSpace(folder))
                return null;

            return Investigate(folder);
        }
    }
}