using SmartRenamer.Models;
using SmartRenamer.Models.Analysis;

namespace SmartRenamer.Services.Intelligence
{
    /// <summary>
    /// Determines whether a project contains
    /// primarily document files.
    /// </summary>
    public class DocumentAnalyzer : IProjectAnalyzer
    {
        public string Name => "Document Analyzer";

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
                ProjectType = "Documents",
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
                Name = "Documents",
                FileCount = folder.DocumentCount,
                Description = "Document files detected."
            });

            if (documentRatio >= 0.60)
            {
                profile.Observations.Add(new ProjectObservation
                {
                    Title = "Document Project",
                    Description =
                        "Most of the files in this folder are documents, suggesting this is primarily a document-based project.",

                    WhyItMatters =
                        "Recognizing the primary purpose of the folder helps Scout make more relevant recommendations.",

                    Severity = ObservationSeverity.Information
                });
            }

            if (folder.DocumentCount > 0)
            {

                if (folder.DocumentCount > 0 &&
    (folder.ImageCount > 0 ||
     folder.VideoCount > 0))
                {
                    profile.Observations.Add(new ProjectObservation
                    {
                        Title = "Mixed Document Collection",

                        Description =
                            $"I found {folder.DocumentCount} document files mixed with other file types.",

                        WhyItMatters =
                            "Keeping documents together can make the project easier to browse and maintain.",

                        Severity = ObservationSeverity.Suggestion
                    });
                }

                profile.Recommendations.Add(new Recommendation
                {
                    Title = "Organize Documents",
                    Description = "Group related documents into a logical folder structure.",
                    Priority = 10
                });

                profile.Recommendations.Add(new Recommendation
                {
                    Title = "Archive Older Documents",
                    Description = "Review older documents that may be ready for archiving.",
                    Priority = 8
                });

                profile.Metadata.Add(new MetadataCapability
                {
                    Name = "Document Metadata",
                    Available = true
                });
            }

            result.Profile = profile;

            return result;
        }
    }
}