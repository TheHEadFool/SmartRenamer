using System.Text;
using SmartRenamer.Models;
using SmartRenamer.Services;

namespace SmartRenamer.Guide
{
    /// <summary>
    /// Connects Scout to the Intelligence Engine.
    /// Scout doesn't investigate folders directly.
    /// Scout asks the workflow to investigate,
    /// analyze, and prepare an organization plan.
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
                return "I wasn't able to investigate that project.";
            }

            ProjectContext context = result.Project;

            StringBuilder summary = new();

            summary.AppendLine("I spent a moment looking through your folder.");
            summary.AppendLine();

            summary.AppendLine($"Project Type: {context.ProjectType}");
            summary.AppendLine($"Confidence: {context.Confidence}%");
            summary.AppendLine();

            summary.AppendLine("Here's what I found:");

            foreach (ProjectObservation observation in context.Observations)
            {
                summary.AppendLine($"• {observation.Description}");
            }

            if (context.RecommendedCapabilities.Count > 0)
            {
                summary.AppendLine();
                summary.AppendLine("I recommend:");

                foreach (string capability in context.RecommendedCapabilities)
                {
                    summary.AppendLine($"✓ {capability}");
                }
            }

            summary.AppendLine();

            summary.AppendLine(
                $"I've already prepared a preliminary organization plan containing {result.Preview.Count:N0} file(s).");

            summary.AppendLine();

            summary.AppendLine(
                "Would you like to preview my recommendations?");

            return summary.ToString();
        }
    }
}