using System.Collections.Generic;
using SmartRenamer.Capabilities.TextReplacement;
using SmartRenamer.Interfaces;
using SmartRenamer.Models;
using SmartRenamer.Models.Rename;

namespace SmartRenamer.Services
{
    /// <summary>
    /// Generates a rename preview using one of
    /// Scout's rename capabilities.
    /// </summary>
    public class RenamePreviewBuilder
    {
        public List<RenamePreview> Build(ProjectContext context)
        {
            List<RenamePreview> preview = new();

            if (context.Folder == null)
                return preview;

            //-------------------------------------------------
            // Temporary:
            // Until Scout begins passing capabilities through
            // the workflow, demonstrate the capability using
            // underscore replacement.
            //-------------------------------------------------

            IRenameCapability capability =
                new TextReplacementCapability();

            TextReplacementRequest request =
                new TextReplacementRequest
                {
                    FindText = "_",
                    ReplaceWith = " "
                };

            preview = capability.BuildPreview(
                context.Folder.Files,
                request);

            return preview;
        }
    }
}