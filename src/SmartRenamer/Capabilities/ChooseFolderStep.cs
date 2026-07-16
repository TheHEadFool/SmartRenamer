using SmartRenamer.Interfaces;
using SmartRenamer.Models;

namespace SmartRenamer.Capabilities
{
    public class ChooseFolderStep : IPipelineStep
    {
        public string Name => "Choose Project Folder";

        public void Execute(FileContext context)
        {
            // Friend will eventually execute this automatically.
            // For now, the Guide Card performs the actual folder selection.
        }
    }
}