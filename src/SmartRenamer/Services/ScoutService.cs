using SmartRenamer.Models;
using SmartRenamer.Models.Rename;

namespace SmartRenamer.Services
{
    /// <summary>
    /// Coordinates execution of Scout's organization plan.
    /// </summary>
    public class ScoutService
    {
        private readonly ScoutExecutor scoutExecutor = new();

        public RenameResult Execute(WorkflowResult workflow)
        {
            return scoutExecutor.Execute(workflow.Preview);
        }
    }
}