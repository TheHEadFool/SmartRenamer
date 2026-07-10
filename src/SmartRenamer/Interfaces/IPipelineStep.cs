using SmartRenamer.Models;

namespace SmartRenamer.Interfaces
{
    public interface IPipelineStep
    {
        string Name { get; }

        void Execute(FileContext context);
    }
}