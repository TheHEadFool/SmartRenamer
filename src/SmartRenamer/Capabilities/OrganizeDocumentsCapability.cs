using SmartRenamer.Interfaces;
using SmartRenamer.Models;

namespace SmartRenamer.Capabilities
{
    public class OrganizeDocumentsCapability : IPipelineStep
    {
        public string Name => "Organize Documents";

        public void Execute(FileContext context)
        {
            // Coming soon
        }
    }
}