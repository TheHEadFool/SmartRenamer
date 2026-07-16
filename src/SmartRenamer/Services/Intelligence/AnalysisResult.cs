using SmartRenamer.Models.Analysis;

namespace SmartRenamer.Services.Intelligence
{
    /// <summary>
    /// Represents the opinion of a single analyzer.
    /// The Intelligence Engine compares these results
    /// to determine the best match.
    /// </summary>
    public class AnalysisResult
    {
        /// <summary>
        /// The analyzer that produced this result.
        /// Example: Photo Analyzer
        /// </summary>
        public string AnalyzerName { get; set; } = "";

        /// <summary>
        /// Confidence from 0 to 100.
        /// </summary>
        public int Confidence { get; set; }

        /// <summary>
        /// The project profile produced by this analyzer.
        /// </summary>
        public ProjectProfile Profile { get; set; } = new();

        /// <summary>
        /// Convenience property.
        /// </summary>
        public bool Success => Confidence > 0;
    }
}