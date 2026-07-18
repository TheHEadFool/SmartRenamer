using SmartRenamer.Models;
using SmartRenamer.Models.Rename;

namespace SmartRenamer.Services
{
    /// <summary>
    /// Coordinates the execution of a rename operation.
    /// </summary>
    public class RenameService
    {
        private readonly RenameExecutor renameExecutor = new();

        /// <summary>
        /// Executes the current rename preview.
        /// </summary>
        public RenameResult Execute(WorkflowResult workflow)
        {
            return renameExecutor.Execute(workflow.Preview);
        }
    }
}