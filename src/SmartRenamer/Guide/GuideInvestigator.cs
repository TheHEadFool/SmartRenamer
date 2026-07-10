using SmartRenamer.Models;
using SmartRenamer.Services;

namespace SmartRenamer.Guide
{
    /// <summary>
    /// Helps the Guide understand a project by asking the
    /// ProjectInvestigator to examine it and then translating
    /// the results into observations Friend can understand.
    /// </summary>
    public class GuideInvestigator
    {
        private readonly ProjectInvestigator projectInvestigator = new();

        /// <summary>
        /// Investigates a project selected by Friend.
        /// </summary>
        public ProjectContext? Investigate()
        {
            return projectInvestigator.Investigate();
        }

        /// <summary>
        /// Converts technical project information into
        /// conversational language.
        /// </summary>
        public string Summarize(ProjectContext context)
        {
            if (context == null)
            {
                return "I wasn't able to investigate the project.";
            }

            FolderSummary folder = context.Folder;

            return
                $"I found {folder.ImageCount:N0} images, " +
                $"{folder.VideoCount:N0} videos, " +
                $"{folder.DocumentCount:N0} documents " +
                $"across {folder.FolderCount:N0} folders.\n\n" +
                "I think I have a good understanding of what we're working with.";
        }
    }
}