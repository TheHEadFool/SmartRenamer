using SmartRenamer.Models;
using SmartRenamer.Models.Analysis;
using SmartRenamer.Observations;

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

            FolderSummary folder = context.Folder;

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

            ProjectProfile profile = new()
            {
                ProjectType = "Music",
                Confidence = score
            };

            result.Confidence = score;

            //--------------------------------------------------
            // Ask the Music Expert to investigate.
            //--------------------------------------------------

            MusicExpert expert = new();
            expert.Observe(folder.FileContexts);

            //--------------------------------------------------
            // Existing observation.
            //--------------------------------------------------

            profile.Observations.Add(new ProjectObservation
            {
                Title = "Audio Collection",

                Description =
                    $"Scout found {musicFiles} supported music files.",

                WhyItMatters =
                    "Recognizing a music collection allows Scout to provide organization recommendations that are appropriate for audio libraries.",

                Severity = ObservationSeverity.Information
            });

            result.Profile = profile;

            return result;
        }
    }
}