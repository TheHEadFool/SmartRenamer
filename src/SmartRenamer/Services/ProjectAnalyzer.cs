using SmartRenamer.Models;
using SmartRenamer.Services.Intelligence;

namespace SmartRenamer.Services
{
    /// <summary>
    /// Compatibility wrapper around the new
    /// Project Intelligence Engine.
    ///
    /// Older parts of the application still call
    /// ProjectAnalyzer.Analyze().
    ///
    /// Internally we now delegate everything to
    /// the Intelligence Engine.
    /// </summary>
    public class ProjectAnalyzer
    {
        private readonly IntelligenceAdapter adapter = new();

        /// <summary>
        /// Analyze the project and populate the
        /// ProjectContext with Scout's understanding.
        /// </summary>
        public void Analyze(ProjectContext context)
        {
            if (context == null)
                return;

            adapter.Analyze(context);
        }
    }
}