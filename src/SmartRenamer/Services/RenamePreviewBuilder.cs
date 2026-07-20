using System.Collections.Generic;
using SmartRenamer.Capabilities.TextReplacement;
using SmartRenamer.Interfaces;
using SmartRenamer.Models;
using SmartRenamer.Models.Planning;
using SmartRenamer.Models.Rename;

namespace SmartRenamer.Services
{
    /// <summary>
    /// Generates a preview of Scout's planned actions.
    /// </summary>
    public class RenamePreviewBuilder
    {
        public List<RenamePreview> Build(
            ProjectContext context,
            ScoutPlan plan)
        {
            List<RenamePreview> preview = new();

            if (context?.Folder == null)
                return preview;

            // New path:
            // If FileContexts are available, build the preview directly
            // from them so Scout can preview organization as well as
            // filename changes.
            if (context.Folder.FileContexts.Count > 0)
            {
                foreach (FileContext file in context.Folder.FileContexts)
                {
                    System.Diagnostics.Debug.WriteLine(
     $"PREVIEW: {file.CurrentName}  Folder='{file.DestinationFolder}'");

                    preview.Add(new RenamePreview
                    {
                        FullPath = file.CurrentFullPath,
                        CurrentName = file.CurrentName,
                        NewName = file.DestinationName,
                        DestinationFolder = file.DestinationFolder,
                        DestinationPath = string.IsNullOrWhiteSpace(file.DestinationFolder)
                            ? ""
                            : System.IO.Path.Combine(
                                file.DestinationFolder,
                                file.DestinationName)
                    });
                }

                return preview;
            }

            // Legacy path:
            // Continue supporting the existing rename workflow until
            // every pipeline produces FileContexts.
            IRenameCapability capability =
                new TextReplacementCapability();

            TextReplacementRequest request = new();

            return capability.BuildPreview(
                context.Folder.Files,
                request);
        }
    }
}