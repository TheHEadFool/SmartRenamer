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
                    Description = "I didn't find any filenames that need changing.",
                    ActionText = "Analyze Another Folder",
                    Priority = 100
                });
            }
            else
            {
                recommendations.Add(new Recommendation
                {
                    Capability = "Analysis",
                    Title = "Ready to Rename",
                    Description = $"Scout found {changeCount} filename(s) that can be renamed.",
                    Priority = 100
                });

                recommendations.Add(new Recommendation
                {
                    ActionId = "RenameFiles",
                    Capability = "Rename",
                    Title = "Rename Files",
                    Description = "Apply the approved filename changes.",
                    ActionText = "Rename",
                    Priority = 90
                });

                recommendations.Add(new Recommendation
                {
                    ActionId = "ExplainChanges",
                    Capability = "Guide",
                    Title = "Explain These Changes",
                    Description = "I'll explain why each filename was recommended for renaming.",
                    ActionText = "Explain",
                    Priority = 80
                });
            }

            return recommendations;
        }
    }
}