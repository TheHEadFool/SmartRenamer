using SmartRenamer.Models;
using SmartRenamer.Models.Analysis;

namespace SmartRenamer.Services.Intelligence
{
    /// <summary>
    /// Determines whether a project appears to be
    /// primarily a photography collection.
    /// </summary>
    public class PhotoAnalyzer : IProjectAnalyzer
    {
        public string Name => "Photo Analyzer";

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

            double imageRatio =
                (double)folder.ImageCount / folder.FileCount;

            // Score from 0–100
            result.Confidence = (int)(imageRatio * 100);

            ProjectProfile profile = new()
            {
                ProjectType = "Photography",
                Confidence = result.Confidence
            };

            profile.Statistics.FileCount = folder.FileCount;
            profile.Statistics.FolderCount = folder.FolderCount;
            profile.Statistics.TotalBytes = folder.TotalBytes;
            profile.Statistics.ImageCount = folder.ImageCount;
            profile.Statistics.VideoCount = folder.VideoCount;
            profile.Statistics.DocumentCount = folder.DocumentCount;
            profile.Statistics.OldestDate = folder.OldestFileDate;
            profile.Statistics.NewestDate = folder.NewestFileDate;

            profile.Collections.Add(new ProjectCollection
            {
                Name = "Images",
                FileCount = folder.ImageCount,
                Description = "Image files detected."
            });

            if (folder.ImageCount > 0)
            {
                profile.Recommendations.Add(new Recommendation
                {
                    Title = "Read EXIF Metadata",
                    Description = "Camera and date information can be extracted.",
                    Priority = 10
                });

                profile.Recommendations.Add(new Recommendation
                {
                    Title = "Rename by Capture Date",
                    Description = "Organize photos chronologically.",
                    Priority = 9
                });

                profile.Recommendations.Add(new Recommendation
                {
                    Title = "Detect Duplicate Photos",
                    Description = "Look for duplicate image files.",
                    Priority = 8
                });

                profile.Metadata.Add(new MetadataCapability
                {
                    Name = "EXIF Metadata",
                    Available = true
                });
            }

            result.Profile = profile;

            return result;
        }
    }
}