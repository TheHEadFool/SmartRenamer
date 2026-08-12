using SmartRenamer.Models;
using SmartRenamer.Models.Analysis;

namespace SmartRenamer.Services.Intelligence
{
    /// <summary>
    /// =========================================================================
    /// MusicAnalyzer
    /// =========================================================================
    ///
    /// Determines whether a project appears to be primarily a music collection.
    ///
    /// =========================================================================
    /// PROJECT STATUS
    /// =========================================================================
    ///
    /// CURRENT ROLE
    /// -------------------------------------------------------------------------
    /// MusicAnalyzer performs project-level classification.
    ///
    /// It does NOT directly invoke MusicExpert.
    ///
    /// The Observation Framework now owns Expert execution:
    ///
    ///     ProjectWorkflow
    ///          ↓
    ///     ObservationEngine
    ///          ↓
    ///     MusicExpert
    ///
    /// This separation is intentional.
    ///
    /// CURRENT MILESTONE
    /// -------------------------------------------------------------------------
    /// Preserve the existing Music Analyzer behavior while allowing the new
    /// Observation Framework to become the single path to MusicExpert.
    ///
    /// FUTURE MUSIC WORK
    /// -------------------------------------------------------------------------
    /// MusicExpert will later be rebuilt through Scout's Expert-creation
    /// process. This analyzer should remain focused on recognizing the
    /// project type rather than becoming a second Music Expert.
    ///
    /// DO NOT
    /// -------------------------------------------------------------------------
    /// • Call MusicExpert directly from this analyzer.
    /// • Add music-domain investigation here.
    /// • Add recommendation logic here.
    ///
    /// =========================================================================
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

            FolderSummary folder = context.Folder;

            //--------------------------------------------------
            // Identify music files.
            //--------------------------------------------------

            int musicFiles = 0;

            foreach (FileContext file in folder.FileContexts)
            {
                switch (file.Extension)
                {
                    case ".mp3":
                    case ".flac":
                    case ".wav":
                    case ".aac":
                    case ".m4a":
                    case ".ogg":
                    case ".wma":
                        musicFiles++;
                        break;
                }
            }

            //--------------------------------------------------
            // Determine whether this appears to be a music
            // collection.
            //--------------------------------------------------

            int score = 0;

            if (musicFiles >= 10)
                score += 40;

            if (musicFiles >= 50)
                score += 30;

            if (folder.DocumentCount == 0)
                score += 10;

            if (folder.ImageCount == 0)
                score += 10;

            if (folder.VideoCount == 0)
                score += 10;

            if (score > 100)
                score = 100;

            //--------------------------------------------------
            // Build project profile.
            //--------------------------------------------------

            ProjectProfile profile = new()
            {
                ProjectType = "Music",
                Confidence = score
            };

            result.Confidence = score;

            //--------------------------------------------------
            // Project-level observation.
            //--------------------------------------------------
            //
            // MusicExpert is intentionally NOT called here.
            //
            // The ObservationEngine is now the single integration
            // point for domain Experts. This prevents MusicAnalyzer
            // and MusicExpert from becoming competing paths.
            //--------------------------------------------------

            profile.Observations.Add(new ProjectObservation
            {
                Title = "Audio Collection",

                Description =
                    "Scout recognized patterns consistent with an audio collection.",

                WhyItMatters =
                    "Recognizing a music collection allows Scout to provide organization recommendations that are appropriate for audio libraries.",

                Severity = ObservationSeverity.Information
            });

            result.Profile = profile;

            return result;
        }
    }
}