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
    }
}