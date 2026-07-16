
using SmartRenamer.Models.Planning;
using SmartRenamer.Models.Rename;

namespace SmartRenamer.Models
{
    /// <summary>
    /// Returned by the workflow after Scout has
    /// investigated the project and built a plan.
    /// </summary>
    public class WorkflowResult
    {
        public ProjectContext Project { get; set; } = new();

        public ScoutPlan Plan { get; set; } = new();

        public System.Collections.Generic.List<RenamePreview> Preview { get; set; }
            = new();
    }
}
