using System.Collections.Generic;
using System.Linq;
using SmartRenamer.Models;

namespace SmartRenamer.Services.Intelligence
{
    /// <summary>
    /// Coordinates every project analyzer and chooses
    /// the analyzer that best understands the project.
    /// </summary>
    public class ProjectIntelligenceEngine
    {
        private readonly List<IProjectAnalyzer> analyzers = new();

        public ProjectIntelligenceEngine()
        {
            analyzers.Add(new PhotoAnalyzer());
            analyzers.Add(new MovieAnalyzer());
            analyzers.Add(new BookAnalyzer());
            analyzers.Add(new MusicAnalyzer());
            analyzers.Add(new DownloadAnalyzer());
        }

        /// <summary>
        /// Runs every analyzer and returns the
        /// highest-confidence result.
        /// </summary>
        public AnalysisResult Analyze(ProjectContext context)
        {
            List<AnalysisResult> results = new();

            foreach (IProjectAnalyzer analyzer in analyzers)
            {
                AnalysisResult result = analyzer.Analyze(context);
                results.Add(result);
            }

            if (results.Count == 0)
            {
                return new AnalysisResult();
            }

            return results
                .OrderByDescending(r => r.Confidence)
                .First();
        }

        /// <summary>
        /// Returns every analyzer's opinion.
        /// Useful later for diagnostics.
        /// </summary>
        public List<AnalysisResult> AnalyzeAll(ProjectContext context)
        {
            List<AnalysisResult> results = new();

            foreach (IProjectAnalyzer analyzer in analyzers)
            {
                results.Add(analyzer.Analyze(context));
            }

            return results;
        }
    }
}