using System.Text;
using SmartRenamer.Models;
using SmartRenamer.Services;

namespace SmartRenamer.Guide
{
    /// <summary>
    /// Connects Scout to the Intelligence Engine.
    /// Scout doesn't investigate folders directly.
    /// Scout asks the workflow to investigate,
    /// analyze, and prepare a rename preview.
    /// </summary>
    public class GuideInvestigator
    {
        private readonly ProjectWorkflow workflow = new();

        /// <summary>
        /// Runs the complete intelligence workflow.
        /// </summary>
        public WorkflowResult? Investigate()
        {
            return workflow.Execute();
        }

        /// <summary>
        /// Converts the workflow results into
        /// conversational language.
        /// </summary>
        public string Summarize(WorkflowResult result)
        {
            if (result == null || result.Project == null)
            {
                return "I wasn't able to analyze that folder.";
            }

            ProjectContext context = result.Project;

            StringBuilder summary = new();

            summary.AppendLine("I've finished analyzing your folder.");
            summary.AppendLine();

            int renameCount = result.Preview.Count;

            if (renameCount == 0)
            {
                summary.AppendLine(
                    "I didn't find any filenames that need changing.");
            }
            else if (renameCount == 1)
            {
                summary.AppendLine(
                    "I found 1 filename that could be improved.");
            }
            else
            {
                summary.AppendLine(
                    $"I found {renameCount:N0} filenames that could be improved.");
            }

            summary.AppendLine();

            if (context.Observations.Count > 0)
            {
                summary.AppendLine("Here's what I noticed:");

                foreach (ProjectObservation observation in context.Observations)
                {
                    summary.AppendLine($"• {observation.Description}");
                }

                summary.AppendLine();
            }

            if (context.RecommendedCapabilities.Count > 0)
            {
                summary.AppendLine("My recommendations:");

                foreach (string capability in context.RecommendedCapabilities)
                {
                    summary.AppendLine($"• {capability}");
                }

                summary.AppendLine();
            }

            if (renameCount == 0)
            {
                summary.AppendLine(
                    "There isn't anything I'd recommend renaming right now.");
            }
            else
            {
                summary.AppendLine(
                    "I've already prepared a preview of the proposed filename changes.");
                summary.AppendLine();
                summary.AppendLine(
                    "We can refine those changes together before anything is renamed.");
                summary.AppendLine();
                summary.AppendLine(
                    "What would you like me to do?");
            }

            return summary.ToString();
        }
    }
}