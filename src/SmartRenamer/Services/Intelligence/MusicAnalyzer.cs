using SmartRenamer.Models;
using SmartRenamer.Models.Analysis;

namespace SmartRenamer.Services.Intelligence
{
    /// <summary>
    /// Determines whether a project appears to be
    /// primarily a music collection.
    /// </summary>
    public class MusicAnalyzer : IProjectAnalyzer
    {
        public string Name => "Music Analyzer";

        public AnalysisResult Analyze(ProjectContext context)
        {
            AnalysisResult result = new()
            {
                AnalyzerName = Name
            };

            if (context == null || context.Folder == null)
                return result;

            // Version 0.4 placeholder.
            // We'll detect MP3, FLAC, AAC, WAV, M4A,
            // album folders and ID3 tags in Version 0.5.

            ProjectProfile profile = new()
            {
                ProjectType = "Music",
                Confidence = 0
            };

            result.Profile = profile;

            return result;
        }
    }
}