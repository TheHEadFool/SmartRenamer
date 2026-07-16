using SmartRenamer.Models;
using SmartRenamer.Models.Analysis;

namespace SmartRenamer.Services.Intelligence
{
    /// <summary>
    /// Determines whether a project appears to be
    /// primarily a book or document collection.
    /// </summary>
    public class BookAnalyzer : IProjectAnalyzer
    {
        public string Name => "Book Analyzer";

        public AnalysisResult Analyze(ProjectContext context)
        {
            AnalysisResult result = new()
            {
                AnalyzerName = Name
            };

            if (context == null || context.Folder == null)
                return result;

            FolderSummary folder = context.Folder;

            if (folder.FileCount == 0)
                return result;

            double documentRatio =
                (double)folder.DocumentCount / folder.FileCount;

            result.Confidence = (int)(documentRatio * 100);

            ProjectProfile profile = new()
            {
                ProjectType = "Books & Documents",
                Confidence = result.Confidence
            };

            profile.Statistics.FileCount = folder.FileCount;
            profile.Statistics.FolderCount = folder.FolderCount;
            profile.Statistics.TotalBytes = folder.TotalBytes;
            profile.Statistics.DocumentCount = folder.DocumentCount;

            if (folder.DocumentCount > 0)
            {
                profile.Collections.Add(new ProjectCollection
                {
                    Name = "Documents",
                    FileCount = folder.DocumentCount,
                    Description = "Document files detected."
                });

                profile.Recommendations.Add(new Recommendation
                {
                    Title = "Organize Documents",
                    Description = "Group documents into a consistent folder structure.",
                    Priority = 10
                });
            }

            result.Profile = profile;

            return result;
        }
    }
}