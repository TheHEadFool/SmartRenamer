using System;
using System.Collections.Generic;

namespace Scout.Observations.Conversation
{
    /// <summary>
    /// =========================================================================
    /// CV_RecommendationSelector
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Choose the next conversation."
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Selects which recommendation Scout should discuss next.
    ///
    /// The selector never creates recommendations.
    /// It simply chooses one from those already produced by Experts.
    ///
    /// Future Responsibilities
    /// -------------------------------------------------------------------------
    /// • Choose the highest priority recommendation.
    /// • Skip completed recommendations.
    /// • Avoid repeating previous conversations.
    /// • Respect user preferences.
    /// • Support future AI-assisted prioritization.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Analyze files.
    /// • Translate ExpertFindings.
    /// • Speak to the user.
    /// • Store conversation history.
    ///
    /// Those responsibilities belong to Experts,
    /// Recommendation Translators,
    /// the Conversation Planner,
    /// and Conversation History.
    /// =========================================================================
    /// </summary>
    public sealed class CV_RecommendationSelector
    {
        public CV_Recommendation? Select(
            IReadOnlyList<CV_Recommendation> recommendations)
        {
            if (recommendations == null)
                throw new ArgumentNullException(nameof(recommendations));

            if (recommendations.Count == 0)
                return null;

            // Generation 1:
            // Simply return the first recommendation.
            // Future generations will prioritize based on
            // confidence, urgency, user intent, history,
            // and AI reasoning.
            return recommendations[0];
        }
    }
}