using System;
using System.Collections.Generic;
using System.IO;
using SmartRenamer.Models.Rename;

namespace SmartRenamer.Services
{
    /// <summary>
    /// Executes an approved rename preview.
    /// </summary>
    public class RenameExecutor
    {
        public RenameResult Execute(List<RenamePreview> preview)
        {
            RenameResult result = new();

            if (preview == null || preview.Count == 0)
            {
                result.Errors.Add("Nothing to rename.");
                return result;
            }

            //-------------------------------------------------
            // Keep track of destinations so we don't try to
            // rename two files to the same filename.
            //-------------------------------------------------

            HashSet<string> destinationPaths =
                new(StringComparer.OrdinalIgnoreCase);

            foreach (RenamePreview item in preview)
            {
                if (!item.WillRename)
                    continue;

                if (!File.Exists(item.FullPath))
                {
                    result.Errors.Add(
                        $"File not found: {item.CurrentName}");

                    result.FilesSkipped++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.NewName))
                {
                    result.Errors.Add(
                        $"Invalid filename: {item.CurrentName}");

                    result.FilesSkipped++;
                    continue;
                }

                string destinationPath =
                    Path.Combine(
                        Path.GetDirectoryName(item.FullPath)!,
                        item.NewName);

                if (!destinationPaths.Add(destinationPath))
                {
                    result.Errors.Add(
                        $"Duplicate destination: {item.NewName}");

                    result.FilesSkipped++;
                    continue;
                }

                if (File.Exists(destinationPath) &&
                    !destinationPath.Equals(
                        item.FullPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add(
                        $"Skipped (already exists): {item.NewName}");

                    result.FilesSkipped++;
                    continue;
                }

                //-------------------------------------------------
                // Safe to rename.
                //-------------------------------------------------

                try
                {
                    File.Move(
                        item.FullPath,
                        destinationPath);

                    result.Transaction.Entries.Add(
                        new RenameTransactionEntry
                        {
                            OriginalPath = item.FullPath,
                            NewPath = destinationPath
                        });

                    result.FilesRenamed++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(
                        $"{item.CurrentName}: {ex.Message}");

                    result.FilesSkipped++;
                }
            }

            result.Success = result.FilesRenamed > 0;

            return result;
        }
    }
}