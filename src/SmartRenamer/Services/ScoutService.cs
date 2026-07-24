using SmartRenamer.Models;
using SmartRenamer.Models.Rename;
using System;

namespace SmartRenamer.Services
{
    /// <summary>
    /// Coordinates execution of Scout's organization plan.
    /// </summary>
    public class ScoutService
    {
        private readonly ScoutExecutor scoutExecutor = new();

        public RenameResult Execute(
            WorkflowResult workflow,
            ScoutOperation operation)
        {
            operation.Status = "Preparing an organized copy...";
            operation.CurrentTask = "Starting...";
            operation.CompletedSteps = 0;

            Progress<ExecutionProgress> progress =
                new(p =>
                {
                    operation.CompletedSteps = p.Completed;
                    operation.TotalSteps = p.Total;
                    operation.CurrentFile = p.CurrentFile;
                    operation.Status = p.Status;
                    operation.CurrentTask = $"Processing {p.Completed:N0} of {p.Total:N0}";
                });

            RenameResult result =
                scoutExecutor.Execute(
                    workflow.Preview,
                    progress);

            operation.Status = "Finished";
            operation.CurrentTask = "Organization complete.";
            operation.State = ScoutOperationState.Completed;

            return result;
        }
    }
}