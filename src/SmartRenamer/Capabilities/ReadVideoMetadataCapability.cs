using SmartRenamer.Interfaces;
using SmartRenamer.Models;

namespace SmartRenamer.Capabilities
{
    public class ReadVideoMetadataCapability : IPipelineStep
    {
        public string Name => "Read Video Metadata";

        public void Execute(FileContext context)
        {
            // Coming soon
        }
    }
}