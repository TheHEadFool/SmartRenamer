using SmartRenamer.Models;
using SmartRenamer.Models.Analysis;

namespace SmartRenamer.Services.Intelligence
{
    /// <summary>
    /// Determines whether a project appears to be
    /// primarily a movie collection.
    /// </summary>
    public class MovieAnalyzer : IProjectAnalyzer
    {
        public string Name => "Movie Analyzer";

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

            double videoRatio =
                (double)folder.VideoCount / folder.FileCount;

            result.Confidence = (int)(videoRatio * 100);

            ProjectProfile profile = new()
            {
                ProjectType = "Movies",
                Confidence = result.Confidence
            };

            profile.Statistics.FileCount = folder.FileCount;
            profile.Statistics.FolderCount = folder.FolderCount;
            profile.Statistics.TotalBytes = folder.TotalBytes;
            profile.Statistics.VideoCount = folder.VideoCount;

            if (folder.VideoCount > 0)
            {
                profile.Collections.Add(new ProjectCollection
                {
                    Name = "Movies",
                    FileCount = folder.VideoCount,
                    Description = "Video files detected."
                });

                profile.Recommendations.Add(new Recommendation
                {
                    Title = "Organize Movie Library",
                    Description = "Group videos into a consistent folder structure.",
                    Priority = 10
                });
            }

            result.Profile = profile;

            return result;
        }
    }
}