using SmartRenamer.Models;
using SmartRenamer.Models.Planning;

namespace SmartRenamer.Services
{
    /// <summary>
    /// Builds Scout's proposed organization plan.
    /// No files are modified here.
    /// Scout simply decides what it intends to do.
    /// </summary>
    public class ScoutPlanner
    {
        public ScoutPlan Build(ProjectContext context)
        {
            ScoutPlan plan = new();

            if (context.Folder == null)
                return plan;

            plan.Goal = "Organize Folder";

            plan.ProtectOriginals = true;

            plan.OrganizeFolders = true;

            plan.RenameFiles = false;

            plan.DestinationFolder =
                System.IO.Path.GetFileName(context.Folder.FolderPath) + " Organized";

            plan.OrganizationStrategy =
                "Keep similar files together.";

            plan.NamingStrategy =
                "Keep current filenames.";

            plan.FilesToProcess =
                context.Folder.Files.Count;

            plan.FilesToRename = 0;

            plan.FilesToMove =
                context.Folder.Files.Count;

            plan.FilesToCopy =
                context.Folder.Files.Count;

            plan.FilesToDelete = 0;

            plan.FoldersToCreate = 1;

            plan.Reasoning.Add(
                "You asked Scout to organize this folder.");

            plan.Reasoning.Add(
                "Scout will protect your originals by creating an organized copy.");

            plan.Recommendations.Add(
                "Review the proposed organization before execution.");

            return plan;
        }
    }
}