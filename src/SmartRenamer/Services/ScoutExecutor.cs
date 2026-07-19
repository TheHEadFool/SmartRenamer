using System;
using System.Collections.Generic;
using System.IO;
using SmartRenamer.Models.Rename;

namespace SmartRenamer.Services
{
    public class ScoutExecutor
    {
        public RenameResult Execute(List<RenamePreview> preview)
        {
            RenameResult result = new();

            if (preview == null || preview.Count == 0)
            {
                result.Errors.Add("Nothing to organize.");
                return result;
            }

            string sourceFolder =
                Path.GetDirectoryName(preview[0].FullPath)!;

            string scoutFolder =
                sourceFolder + " (Scout Organized)";

            Directory.CreateDirectory(scoutFolder);

            foreach (RenamePreview item in preview)
            {
                if (!File.Exists(item.FullPath))
                {
                    result.Errors.Add(
                        $"File not found: {item.CurrentName}");

                    result.FilesSkipped++;
                    continue;
                }

                string destination =
                    string.IsNullOrWhiteSpace(item.DestinationFolder)
                        ? Path.Combine(
                            scoutFolder,
                            item.NewName)
                        : Path.Combine(
                            scoutFolder,
                            item.DestinationFolder,
                            item.NewName);

                string? destinationDirectory =
    Path.GetDirectoryName(destination);

                if (!string.IsNullOrWhiteSpace(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                File.Copy(
                    item.FullPath,
                    destination,
                    true);

                result.FilesRenamed++;
            }

            result.Success = result.FilesRenamed > 0;

            return result;
        }
    }
}