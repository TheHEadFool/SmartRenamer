using System.Collections.Generic;
using SmartRenamer.Capabilities.TextReplacement;
using SmartRenamer.Interfaces;
using SmartRenamer.Models;
using SmartRenamer.Models.Planning;
using SmartRenamer.Models.Rename;

namespace SmartRenamer.Services
{
    /// <summary>
    /// Generates a rename preview using Scout's
    /// current plan.
    /// </summary>
    public class RenamePreviewBuilder
    {
        public List<RenamePreview> Build(
            ProjectContext context,
            ScoutPlan plan)
        {
            List<RenamePreview> preview = new();

            if (context.Folder == null)
                return preview;

            IRenameCapability capability =
                new TextReplacementCapability();

            // Temporary:
            // Until Scout can build filenames directly,
            // continue using the existing text replacement
            // capability.
            TextReplacementRequest request = new();

            preview = capability.BuildPreview(
                context.Folder.Files,
                request);

            return preview;
        }
    }
}