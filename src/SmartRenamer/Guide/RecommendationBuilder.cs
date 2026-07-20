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
                    Title = "I Found Related Files",
                    Description =
                        $"I found {changeCount:N0} file(s) that appear to belong together.\n\n" +
                        "They share similar names but use different extensions or naming styles.\n\n" +
                        "I can organize them into a single folder while leaving your original files untouched.",
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