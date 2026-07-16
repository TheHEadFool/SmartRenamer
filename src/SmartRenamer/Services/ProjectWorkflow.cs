using SmartRenamer.Models;
using SmartRenamer.Models.Planning;

namespace SmartRenamer.Services
{
    /// <summary>
    /// Coordinates Scout's complete workflow.
    /// </summary>
    public class ProjectWorkflow
    {
        private readonly ProjectInvestigator investigator = new();

        private readonly ProjectAnalyzer analyzer = new();

        private readonly ScoutPlanner planner = new();

        private readonly RenamePreviewBuilder previewBuilder = new();

        public WorkflowResult? Execute()
        {
            ProjectContext? context = investigator.Investigate();

            if (context == null)
                return null;

            analyzer.Analyze(context);

            ScoutPlan plan = planner.Build(context);

            var preview = previewBuilder.Build(context);

            plan.RenamePreview.AddRange(preview);

            return new WorkflowResult
            {
                Project = context,
                Plan = plan,
                Preview = preview
            };
        }
    }
}