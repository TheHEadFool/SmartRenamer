using SmartRenamer.Interfaces;
using SmartRenamer.Models;

namespace SmartRenamer.Capabilities
{
    public class ReadExifCapability : IPipelineStep
    {
        public string Name => "Read EXIF Metadata";

        public void Execute(FileContext context)
        {
            // Coming soon
        }
    }
}