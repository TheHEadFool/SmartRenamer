using SmartRenamer.Services.Naming;
using SmartRenamer.Models;
using SmartRenamer.Models.Planning;
using System;
using System.Linq;

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
                "Organize photos by capture date.";

            plan.NamingStrategy =
    "Improve filenames when high-confidence suggestions are available.";

            // NEW: Decide where each photo belongs.
            FilenameAnalysisEngine filenameEngine = new();
            foreach (FileContext file in context.Folder.FileContexts)
            {
                FilenameAnalysisResult analysis =
                    filenameEngine.Analyze(file.CurrentName);

                file.DestinationName = analysis.Suggestions
                    .Where(s => s.Confidence == SuggestionConfidence.High)
                    .Select(s => s.ProposedName)
                    .FirstOrDefault()
                    ?? file.CurrentName;

                if (file.Variables.TryGetValue("CaptureDate", out object? value) &&
                    value is DateTime captureDate)
                {
                    file.DestinationFolder = System.IO.Path.Combine(
                        plan.DestinationFolder,
                        captureDate.Year.ToString(),
                        $"{captureDate:yyyy-MM MMMM}");
                }
                else
                {
                    file.DestinationFolder = System.IO.Path.Combine(
                        plan.DestinationFolder,
                        "Unknown Date");
                }
            }

            plan.FilesToProcess = context.Folder.FileContexts.Count;
            plan.FilesToRename = 0;
            plan.FilesToMove = context.Folder.FileContexts.Count;
            plan.FilesToCopy = context.Folder.FileContexts.Count;
            plan.FilesToDelete = 0;

            // Count the unique folders Scout intends to create.
            plan.FoldersToCreate = context.Folder.FileContexts
                .Select(f => f.DestinationFolder)
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Distinct()
                .Count();

            plan.Reasoning.Add(
                "Scout organized photos using their capture date when available.");

            plan.Reasoning.Add(
                "Original files remain untouched.");

            plan.Recommendations.Add(
                "Review the destination folders before copying files.");

            return plan;
        }
    }
}