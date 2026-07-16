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

            context.Observations.Add(new ProjectObservation
            {
                Description =
                    $"Detected project type: {profile.ProjectType}"
            });

            context.Observations.Add(new ProjectObservation
            {
                Description =
                    $"Confidence: {profile.Confidence}%"
            });

            context.RecommendedCapabilities.Clear();

            foreach (Recommendation recommendation in profile.Recommendations)
            {
                context.RecommendedCapabilities.Add(
                    recommendation.Title);
            }
        }
    }
}