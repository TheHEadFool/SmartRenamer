using SmartRenamer.Interfaces;
using SmartRenamer.Models;

namespace SmartRenamer.Capabilities
{
    public class DetectDuplicateFilesCapability : IPipelineStep
    {
        public string Name => "Detect Duplicate Files";

        public void Execute(FileContext context)
        {
            // Coming soon
        }
    }
}
