using SmartRenamer.Models;
using SmartRenamer.Models.Analysis;

namespace SmartRenamer.Services
{
    /// <summary>
    /// Converts project observations into
    /// user-friendly recommendations.
    /// Scout never invents recommendations.
    /// Everything comes from the ProjectContext.
    /// </summary>
    public class RecommendationBuilder
    {
        public void Build(ProjectContext context)
        {
            context.Recommendations.Clear();

            switch (context.ProjectType)
            {
                case "Photography":
                    BuildPhotographyRecommendations(context);
                    break;

                case "Movies":
                    BuildMovieRecommendations(context);
                    break;

                case "Music":
                    BuildMusicRecommendations(context);
                    break;

                case "Books":
                    BuildBookRecommendations(context);
                    break;

                default:
                    BuildGenericRecommendations(context);
                    break;
            }
        }

        private void BuildPhotographyRecommendations(ProjectContext context)
        {
            context.Recommendations.Add(new Recommendation
            {
                Title = "Organize by Date Taken",

                Description =
                    "Most photos contain the date they were taken.",

                Question =
                    "Would you like me to organize your photos by the day they were taken?",

                Priority = 100,

                RequiresDecision = true
            });

            context.Recommendations.Add(new Recommendation
            {
                Title = "Group by Camera",

                Description =
                    "I found camera information inside many photos.",

                Question =
                    "Would you like separate folders for each camera?",

                Priority = 80,

                RequiresDecision = true
            });

            context.Recommendations.Add(new Recommendation
            {
                Title = "Rename Photos",

                Description =
                    "Replace IMG_1234 with descriptive filenames.",

                Question =
                    "Would you like descriptive photo names?",

                Priority = 60,

                RequiresDecision = true
            });
        }

        private void BuildMovieRecommendations(ProjectContext context)
        {
            context.Recommendations.Add(new Recommendation
            {
                Title = "Organize by Movie",

                Description =
                    "Group related movie files together.",

                Question =
                    "Would you like each movie placed in its own folder?",

                Priority = 100,

                RequiresDecision = true
            });
        }

        private void BuildMusicRecommendations(ProjectContext context)
        {
            context.Recommendations.Add(new Recommendation
            {
                Title = "Organize by Artist",

                Description =
                    "Arrange music by artist and album.",

                Question =
                    "Would you like me to organize by artist?",

                Priority = 100,

                RequiresDecision = true
            });
        }

        private void BuildBookRecommendations(ProjectContext context)
        {
            context.Recommendations.Add(new Recommendation
            {
                Title = "Organize by Author",

                Description =
                    "Books can be grouped by author.",

                Question =
                    "Would you like books grouped by author?",

                Priority = 100,

                RequiresDecision = true
            });
        }

        private void BuildGenericRecommendations(ProjectContext context)
        {
            context.Recommendations.Add(new Recommendation
            {
                Title = "Review Folder",

                Description =
                    "I'll help identify the best organization strategy.",

                Question =
                    "Would you like me to build a suggested organization plan?",

                Priority = 50,

                RequiresDecision = true
            });
        }
    }
}