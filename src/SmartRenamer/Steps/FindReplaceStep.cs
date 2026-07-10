using SmartRenamer.Interfaces;
using SmartRenamer.Models;

namespace SmartRenamer.Steps
{
    public class FindReplaceStep : IPipelineStep
    {
        public string Name => "Find and Replace";

        public string Find { get; set; } = "";

        public string Replace { get; set; } = "";

        public void Execute(FileContext context)
        {
            if (string.IsNullOrWhiteSpace(Find))
                return;

            context.DestinationName = context.DestinationName.Replace(
                Find,
                Replace);
        }
    }
}