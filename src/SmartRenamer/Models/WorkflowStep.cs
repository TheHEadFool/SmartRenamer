using SmartRenamer.Interfaces;

namespace SmartRenamer.Models
{
    public class WorkflowStep
    {
        public IPipelineStep Step { get; }

        public WorkflowStep(IPipelineStep step)
        {
            Step = step;
        }

        public string Name => Step.Name;

        public string Description => "Description coming soon...";

        public string Category => "General";

        public string Icon => "🧩";
    }
}