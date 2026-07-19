using SmartRenamer.Capabilities.TextReplacement;
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

        private readonly CapabilityFactory capabilityFactory = new();

        /// <summary>
        /// Starts a new workflow by asking the user to choose a folder.
        /// </summary>
        public WorkflowResult? Execute()
        {
            ProjectContext? context = investigator.Investigate();

            if (context == null)
                return null;

            return Execute(context);
        }

        /// <summary>
        /// Rebuilds the workflow using an existing project.
        /// This is used after renaming so the preview refreshes.
        /// </summary>
        public WorkflowResult Execute(ProjectContext context)
        {
            analyzer.Analyze(context);

            // Execute the recommended capabilities on every discovered file.
            foreach (string capabilityName in context.RecommendedCapabilities)
            {
                WorkflowStep? workflowStep =
                    capabilityFactory.Create(capabilityName);

                if (workflowStep == null)
                    continue;

                foreach (FileContext file in context.Folder.FileContexts)
                {
                    workflowStep.Step.Execute(file);
                }
            }

            ScoutPlan plan = planner.Build(context);

            var preview = previewBuilder.Build(context, plan);

            plan.RenamePreview.Clear();
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