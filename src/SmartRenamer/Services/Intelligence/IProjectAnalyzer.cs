using SmartRenamer.Models;
using SmartRenamer.Models.Analysis;

namespace SmartRenamer.Services.Intelligence
{
    /// <summary>
    /// Every analyzer examines a project and reports
    /// how strongly it believes the project belongs
    /// to its specialty.
    /// </summary>
    public interface IProjectAnalyzer
    {
        string Name { get; }

        AnalysisResult Analyze(ProjectContext context);
    }
}