using System.Collections.Generic;
using System.IO;
using SmartRenamer.Models;
using SmartRenamer.Models.Rename;

namespace SmartRenamer.Services
{
    /// <summary>
    /// Generates a preview of every filename before
    /// anything is actually renamed.
    /// </summary>
    public class RenamePreviewBuilder
    {
        public List<RenamePreview> Build(ProjectContext context)
        {
            List<RenamePreview> preview = new();

            if (context.Folder == null)
                return preview;

            int number = 1;

            foreach (string file in context.Folder.Files)
            {
                string extension = Path.GetExtension(file);

                RenamePreview item = new RenamePreview
                {
                    FullPath = file,
                    CurrentName = Path.GetFileName(file),
                    NewName = $"File {number:000}{extension}"
                };

                preview.Add(item);

                number++;
            }

            return preview;
        }
    }
}