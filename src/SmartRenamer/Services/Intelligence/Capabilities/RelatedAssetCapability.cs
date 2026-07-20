using SmartRenamer.Models;
using System.IO;

namespace SmartRenamer.Services.Intelligence.Capabilities
{
    /// <summary>
    /// Discovers files that appear to belong together
    /// based on a shared base filename.
    /// This capability discovers facts only.
    /// It never decides where files should be placed.
    /// </summary>
    public class RelatedAssetCapability : ICapability
    {
        public string Name => "Related Asset Capability";

        public void Analyze(ProjectContext context)
        {
            if (context?.Folder == null)
                return;

            foreach (FileContext file in context.Folder.FileContexts)
            {
                string baseName =
                    Path.GetFileNameWithoutExtension(file.CurrentName);

                file.Variables["RelatedGroup"] = baseName;
            }
        }
    }
}