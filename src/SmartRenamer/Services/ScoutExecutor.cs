using SmartRenamer.Models;
using SmartRenamer.Models.Rename;
using System;
using System.Collections.Generic;
using System.IO;

namespace SmartRenamer.Services
{
    public class ScoutExecutor
    {
        public RenameResult Execute(
            List<RenamePreview> preview,
            IProgress<ExecutionProgress>? progress = null)
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

            // Tell the UI where the organized copy lives.
            result.OutputFolder = scoutFolder;

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

                System.Diagnostics.Debug.WriteLine($"SOURCE      : {item.FullPath}");
                System.Diagnostics.Debug.WriteLine($"GROUP       : {item.DestinationFolder}");
                System.Diagnostics.Debug.WriteLine($"NEW NAME    : {item.NewName}");
                System.Diagnostics.Debug.WriteLine($"DESTINATION : {destination}");

                File.Copy(
                    item.FullPath,
                    destination,
                    true);

                result.FilesRenamed++;

                progress?.Report(
                    new ExecutionProgress
                    {
                        Completed = result.FilesRenamed,
                        Total = preview.Count,
                        CurrentFile = item.NewName,
                        Status = "Copying files..."
                    });
            }

            result.Success = result.FilesRenamed > 0;

            return result;
        }
    }
}