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

            //---------------------------------------------------------
            // TEMPORARY LIMITATION — CONVERSATION SELECTION
            //---------------------------------------------------------
            //
            // The Conversation Engine currently receives the complete set of
            // CV_Recommendations, but CV_RecommendationSelector intentionally
            // selects the first recommendation.
            //
            // This is deliberate for the initial Conversation Framework
            // vertical slice. The purpose of this implementation is to prove
            // that an Expert-generated CV_Recommendation can travel:
            //
            //     Expert
            //       ↓
            //     CV_Recommendation
            //       ↓
            //     CV_ConversationEngine
            //       ↓
            //     CV_CurrentTopic
            //       ↓
            //     Workspace
            //
            // DO NOT add prioritization, ranking, scoring, or conversational
            // selection logic here.
            //
            // Future work should improve CV_RecommendationSelector when the
            // Conversation Framework is ready for recommendation prioritization.
            //
            // Until then, "first recommendation wins" is the known and
            // intentional behavior.
            //
            // This comment should be removed or updated when the Selector
            // becomes responsible for real recommendation selection.
            //
            //---------------------------------------------------------
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