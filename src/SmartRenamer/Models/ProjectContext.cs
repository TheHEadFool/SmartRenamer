using SmartRenamer.Services;

namespace SmartRenamer.Models
{
    public class ProjectContext
    {
        public FolderSummary? Folder { get; set; }

        public string ProjectGoal { get; set; } = "";

        public bool KeepOriginals { get; set; } = true;
    }
}