using System.Collections.ObjectModel;
using SmartRenamer.Models;

namespace SmartRenamer.ViewModels
{
    public class PipelineViewModel
    {
        public ObservableCollection<WorkflowStep> Steps { get; }
            = new();

        public void AddStep(WorkflowStep step)
        {
            Steps.Add(step);
        }

        public void RemoveStep(WorkflowStep step)
        {
            Steps.Remove(step);
        }

        public void MoveUp(WorkflowStep step)
        {
            int index = Steps.IndexOf(step);

            if (index > 0)
            {
                Steps.Move(index, index - 1);
            }
        }

        public void MoveDown(WorkflowStep step)
        {
            int index = Steps.IndexOf(step);

            if (index < Steps.Count - 1)
            {
                Steps.Move(index, index + 1);
            }
        }
    }
}