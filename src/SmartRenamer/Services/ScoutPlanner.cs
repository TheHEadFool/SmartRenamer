using SmartRenamer.Models;
using SmartRenamer.Models.Planning;
using SmartRenamer.Services.Naming;
using System;
using System.Collections.Generic;
using System.IO;
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
                Path.GetFileName(context.Folder.FolderPath) + " Organized";

            plan.OrganizationStrategy =
                "Group related assets that share the same filename.";

            plan.NamingStrategy =
                "Improve filenames when high-confidence suggestions are available.";

            FilenameAnalysisEngine filenameEngine = new();

            //---------------------------------------------------------
            // PASS 1
            // Determine the final filename for every file.
            //---------------------------------------------------------

            foreach (FileContext file in context.Folder.FileContexts)
            {
                FilenameAnalysisResult analysis =
                    filenameEngine.Analyze(file.CurrentName);

                file.DestinationName =
                    analysis.Suggestions
                        .Where(s => s.Confidence == SuggestionConfidence.High)
                        .Select(s => s.ProposedName)
                        .FirstOrDefault()
                    ?? file.CurrentName;
            }

            //---------------------------------------------------------
            // PASS 2
            // Count files using their FINAL filenames.
            //---------------------------------------------------------

            Dictionary<string, int> baseNameCounts =
                context.Folder.FileContexts
                    .GroupBy(
                        f => Path.GetFileNameWithoutExtension(f.DestinationName),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Count(),
                        StringComparer.OrdinalIgnoreCase);

            //---------------------------------------------------------
            // PASS 3
            // Assign destination folders.
            //---------------------------------------------------------

            foreach (FileContext file in context.Folder.FileContexts)
            {
                string baseName =
                    Path.GetFileNameWithoutExtension(file.DestinationName);

                if (baseNameCounts.TryGetValue(baseName, out int count) &&
                    count > 1)
                {
                    file.DestinationFolder = baseName;
                }
                else
                {
                    file.DestinationFolder = "";
                }
            }

            plan.FilesToProcess = context.Folder.FileContexts.Count;
            plan.FilesToRename = 0;
            plan.FilesToMove = context.Folder.FileContexts.Count;
            plan.FilesToCopy = context.Folder.FileContexts.Count;
            plan.FilesToDelete = 0;

            plan.FoldersToCreate =
                context.Folder.FileContexts
                    .Select(f => f.DestinationFolder)
                    .Where(f => !string.IsNullOrWhiteSpace(f))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

            plan.Reasoning.Clear();

            plan.Reasoning.Add(
                "Scout grouped related assets that share the same filename.");

            plan.Reasoning.Add(
                "Original files remain untouched.");

            plan.Recommendations.Clear();

            plan.Recommendations.Add(
                "Review the proposed organization before copying files.");

            return plan;
        }
    }
}