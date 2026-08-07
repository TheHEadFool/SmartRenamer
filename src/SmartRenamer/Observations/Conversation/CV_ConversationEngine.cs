using System;
using System.Collections.Generic;

namespace Scout.Observations.Conversation;

/// <summary>
/// =========================================================================
/// CV_ConversationEngine
/// =========================================================================
///
/// Motto
/// -------------------------------------------------------------------------
/// "Turn observations into conversations."
///
/// Purpose
/// -------------------------------------------------------------------------
/// Coordinates the Conversation Framework.
///
/// The Conversation Engine owns the state of the current conversation.
/// It does not analyze files or translate ExpertFindings. Those
/// responsibilities belong to the Observation Framework and the
/// Expert Translators.
///
/// Responsibilities
/// -------------------------------------------------------------------------
/// • Own the current conversation.
/// • Own all recommendations.
/// • Select the next recommendation.
/// • Track the current topic.
/// • Coordinate the Conversation Planner.
/// • Maintain conversation history.
/// • Track user intent.
///
/// This class does NOT
/// -------------------------------------------------------------------------
/// • Analyze files.
/// • Translate ExpertFindings.
/// • Render the user interface.
/// =========================================================================
/// </summary>
public sealed class CV_ConversationEngine
{
    //---------------------------------------------------------
    // Conversation Components
    //---------------------------------------------------------

    private readonly CV_RecommendationSelector _selector = new();

    private readonly CV_CurrentTopic _currentTopic = new();

    private readonly CV_ConversationPlanner _planner = new();

    private readonly CV_ConversationHistory _history = new();

    private readonly CV_UserIntent _userIntent = new();

    //---------------------------------------------------------
    // Conversation State
    //---------------------------------------------------------

    private readonly List<CV_Recommendation> _recommendations = new();

    //---------------------------------------------------------
    // Public Properties
    //---------------------------------------------------------

    public CV_CurrentTopic CurrentTopic =>
        _currentTopic;

    public CV_ConversationHistory History =>
        _history;

    public CV_UserIntent UserIntent =>
        _userIntent;

    public IReadOnlyList<CV_Recommendation> Recommendations =>
        _recommendations;

    //---------------------------------------------------------
    // Public Methods
    //---------------------------------------------------------

    /// <summary>
    /// Begins a new conversation using the supplied recommendations.
    /// </summary>
    public void Start(
        IReadOnlyList<CV_Recommendation> recommendations)
    {
        if (recommendations == null)
            throw new ArgumentNullException(nameof(recommendations));

        _recommendations.Clear();

        _recommendations.AddRange(recommendations);

        CV_Recommendation? currentRecommendation =
            _selector.Select(_recommendations);

        if (currentRecommendation != null)
        {
            _currentTopic.Begin(currentRecommendation);
        }
        else
        {
            _currentTopic.Clear();
        }
    }
}