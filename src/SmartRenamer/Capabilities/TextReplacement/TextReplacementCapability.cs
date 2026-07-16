using System.Collections.Generic;
using System.IO;
using SmartRenamer.Interfaces;
using SmartRenamer.Models.Rename;

namespace SmartRenamer.Capabilities.TextReplacement
{
    /// <summary>
    /// Performs text replacement on filenames and
    /// generates a rename preview.
    /// </summary>
    public class TextReplacementCapability : IRenameCapability
    {
        public List<RenamePreview> BuildPreview(
            IEnumerable<string> files,
            object request)
        {
            TextReplacementRequest textRequest =
                (TextReplacementRequest)request;

            List<RenamePreview> preview = new();

            foreach (string file in files)
            {
                string current = Path.GetFileName(file);

                string proposed =
                    current.Replace(
                        textRequest.FindText,
                        textRequest.ReplaceWith);

                if (current == proposed)
                    continue;

                preview.Add(new RenamePreview
                {
                    FullPath = file,
                    CurrentName = current,
                    NewName = proposed
                });
            }

            return preview;
        }
    }
}