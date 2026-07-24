using SmartRenamer.Models;
using SmartRenamer.Models.Analysis;

namespace SmartRenamer.Services.Intelligence
{
    /// <summary>
    /// Bridges the new Project Intelligence Engine with the
    /// existing ProjectContext while preserving compatibility
    /// with the existing application.
    /// </summary>
    public class IntelligenceAdapter
    {
        private readonly ProjectIntelligenceEngine engine = new();

        public void Analyze(ProjectContext context)
        {
            if (context == null)
                return;

            AnalysisResult result = engine.Analyze(context);

            if (result == null)
                return;

            ProjectProfile profile = result.Profile;

            if (profile == null)
                return;

            //--------------------------------------------------
            // Store the complete profile.
            //--------------------------------------------------

            context.Profile = profile;

            //--------------------------------------------------
            // Legacy compatibility.
            //--------------------------------------------------

            context.ProjectType = profile.ProjectType;
            context.Confidence = profile.Confidence;

            context.Observations.Clear();

            // TODO:
            // These default observations exist for backward compatibility.
            // Eventually Scout should display observations produced directly
            // by analyzers instead of generic placeholder messages.

            context.Observations.Add(new ProjectObservation
            {
                Title = "Project Type",
                Description =
                    $"Scout recognized this folder as a {profile.ProjectType} project.",

                WhyItMatters =
                    "Understanding the type of project helps Scout make smarter recommendations and avoid treating every folder the same.",

                Severity = ObservationSeverity.Information
            });

            context.Observations.Add(new ProjectObservation
            {
                Title = "Related Files",
                Description =
                    "Scout found files that appear to belong together and can be organized into a consistent structure.",

                WhyItMatters =
                    "Keeping related files together makes projects easier to understand, maintain, and locate in the future.",

                Severity = ObservationSeverity.Suggestion
            });

            //--------------------------------------------------
            // Analyzer-specific observations
            //--------------------------------------------------

            foreach (ProjectObservation observation in profile.Observations)
            {
                context.Observations.Add(observation);
            }

            context.RecommendedCapabilities.Clear();

            foreach (Recommendation recommendation in profile.Recommendations)
            {
                context.RecommendedCapabilities.Add(
                    recommendation.Title);
            }
        }
    }
}