using SmartRenamer.Models;

namespace SmartRenamer.Services.Intelligence.Capabilities
{
    /// <summary>
    /// A capability discovers facts about files.
    /// It never decides what Scout should do.
    /// </summary>
    public interface ICapability
    {
        string Name { get; }

        void Analyze(ProjectContext context);
    }
}