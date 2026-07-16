using SmartRenamer.Models;
using SmartRenamer.Models.Analysis;

namespace SmartRenamer.Services.Intelligence
{
    /// <summary>
    /// Detects folders that appear to be generic
    /// download folders containing mixed file types.
    /// </summary>
    public class DownloadAnalyzer : IProjectAnalyzer
    {
        public string Name => "Download Analyzer";

        public AnalysisResult Analyze(ProjectContext context)
        {
            AnalysisResult result = new()
            {
                AnalyzerName = Name
            };

            if (context == null || context.Folder == null)
                return result;

            FolderSummary folder = context.Folder;

            // Mixed folders are often Downloads folders.
            int score = 0;

            if (folder.ImageCount > 0)
                score += 20;

            if (folder.VideoCount > 0)
                score += 20;

            if (folder.DocumentCount > 0)
                score += 20;

            if (folder.FileCount > 100)
                score += 20;

            if (folder.FolderCount < 10)
                score += 20;

            if (score > 100)
                score = 100;

            ProjectProfile profile = new()
            {
                ProjectType = "Downloads",
                Confidence = score
            };

            profile.Statistics.FileCount = folder.FileCount;
            profile.Statistics.FolderCount = folder.FolderCount;
            profile.Statistics.TotalBytes = folder.TotalBytes;

            profile.Recommendations.Add(new Recommendation
            {
                Title = "Organize Downloads",
                Description = "This appears to be a mixed collection of files that could be sorted automatically.",
                Priority = 10
            });

            result.Confidence = score;
            result.Profile = profile;

            return result;
        }
    }
}