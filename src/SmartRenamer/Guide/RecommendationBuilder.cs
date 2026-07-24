using System.Collections.Generic;
using System.Linq;
using SmartRenamer.Models;
using SmartRenamer.Models.Recommendations;

namespace SmartRenamer.Guide
{
    /// <summary>
    /// Builds the recommendations Scout presents after analyzing a project.
    /// </summary>
    public class RecommendationBuilder
    {
        public List<Recommendation> Build(WorkflowResult workflow)
        {
            var recommendations = new List<Recommendation>();

            if (workflow == null)
                return recommendations;

            int changeCount = workflow.Preview.Count(p => p.HasChanges);

            string projectType =
    workflow.Project.ProjectType?.Trim() ?? string.Empty;

            string discoveryTitle = "I Found Related Files";
            string discoveryDescription =
                $"I found {changeCount:N0} file(s) that appear to belong together.\n\n" +
                "They share similar names but use different extensions or naming styles.\n\n" +
                "I can organize them into a single folder while leaving your original files untouched.";

            switch (projectType.ToLowerInvariant())
            {
                case "photo library":
                case "photos":
                    discoveryTitle = "I Found a Photo Collection";
                    discoveryDescription =
                        $"I found {changeCount:N0} photo file(s) that appear to belong together.\n\n" +
                        "Scout recognized this as a photo collection and can organize it while preserving your originals.";
                    break;

                case "music library":
                case "music":
                    discoveryTitle = "I Found a Music Library";
                    discoveryDescription =
                        $"I found {changeCount:N0} music file(s) that appear to belong together.\n\n" +
                        "Scout recognized an audio collection and can organize albums and tracks without changing the originals.";
                    break;

                case "software project":
                case "source code":
                    discoveryTitle = "I Found a Software Project";
                    discoveryDescription =
                        $"I found {changeCount:N0} project file(s) that belong together.\n\n" +
                        "Scout recognized a software project and will preserve the project structure while organizing supporting assets.";
                    break;
            }

            if (changeCount == 0)
            {
                recommendations.Add(new Recommendation
                {
                    ActionId = "AnalyzeAnotherFolder",
                    Capability = "Analysis",
                    Title = "Everything Looks Good",
                    Description =
                        "I didn't find any filenames that need organizing or standardizing.",
                    ActionText = "Analyze Another Folder",
                    Priority = 100
                });
            }
            else
            {
                recommendations.Add(new Recommendation
                {
                    Capability = "Analysis",
                    Title = discoveryTitle,
                    Description = discoveryDescription,
                    EstimatedChanges = changeCount,
                    Priority = 100
                });

                recommendations.Add(new Recommendation
                {
                    ActionId = "RenameFiles",
                    Capability = "Organization",
                    Title = "Organize Related Assets",
                    Description =
                        "I'll create an organized copy of your project, group related files together, and preserve every original file.",
                    ActionText = "Organize",
                    EstimatedChanges = changeCount,
                    Priority = 90
                });

                recommendations.Add(new Recommendation
                {
                    ActionId = "ExplainChanges",
                    Capability = "Guide",
                    Title = "Show Me Why",
                    Description =
                        "I'll explain why I made each recommendation before anything is changed.",
                    ActionText = "Explain",
                    Priority = 80
                });
            }

            return recommendations;
        }
    }
}