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
/// Review All has a deliberately different purpose from normal conversation:
///
///     Normal conversation
///         One recommendation
///              ↓
///         Scout discusses it
///
///     Review All
///         ALL recommendations
///              ↓
///         Complete report
///              ↓
///         User chooses what to discuss with Scout
///
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

    private int _currentRecommendationIndex = -1;

    private readonly List<CV_ReviewAllItem> _reviewAllItems = new();

    private readonly List<CV_ActionOption> _pendingActionOptions = new();

    public void RememberActionOptions(IReadOnlyList<CV_ActionOption> options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        _pendingActionOptions.Clear();
        _pendingActionOptions.AddRange(options);
    }

    /// <summary>
    /// Clears the action options that are waiting for a user selection.
    /// The current selection has been consumed and should not remain active.
    /// </summary>
    public void ClearActionOptions()
    {
        _pendingActionOptions.Clear();
    }

    private bool _reviewingAll;
    private bool _awaitingReviewAllApproval;

    //---------------------------------------------------------
    // Public State
    //---------------------------------------------------------

    /// <summary>
    /// True while the complete Review All report is active.
    /// </summary>
    public bool IsReviewingAll =>
        _reviewingAll;

    /// <summary>
    /// True when Scout has asked the user whether to open
    /// the complete Review All report and is waiting for
    /// the user's answer.
    /// </summary>
    public bool IsAwaitingReviewAllApproval =>
        _awaitingReviewAllApproval;

    /// <summary>
    /// Zero-based index of the recommendation currently being discussed.
    ///
    /// This remains available for normal conversation and future
    /// conversational navigation.
    /// </summary>
    public int CurrentRecommendationIndex =>
        _currentRecommendationIndex;

    /// <summary>
    /// The recommendation currently being discussed by Scout.
    /// </summary>
    public CV_Recommendation? CurrentRecommendation =>
        _currentTopic.Recommendation;

    //---------------------------------------------------------
    // Public Properties
    //---------------------------------------------------------

    /// <summary>
    /// The topic currently being discussed.
    /// </summary>
    public CV_CurrentTopic CurrentTopic =>
        _currentTopic;


    /// <summary>
    /// Conversation history.
    /// </summary>
    public CV_ConversationHistory History =>
        _history;

    /// <summary>
    /// Current user intent.
    /// </summary>
    public CV_UserIntent UserIntent =>
        _userIntent;

    /// <summary>
    /// All recommendations currently owned by the conversation.
    /// </summary>
    public IReadOnlyList<CV_Recommendation> Recommendations =>
        _recommendations;

    /// <summary>
    /// Complete Review All report.
    ///
    /// This contains every recommendation discovered during the current
    /// observation pass.
    ///
    /// Review All does NOT throw these away after the first item.
    /// </summary>
    public IReadOnlyList<CV_ReviewAllItem> ReviewAllItems =>
        _reviewAllItems;

    //---------------------------------------------------------
    // Start
    //---------------------------------------------------------

    /// <summary>
    /// Begins a new conversation using the supplied recommendations.
    ///
    /// This selects the initial recommendation normally.
    /// It does not enter Review All mode.
    /// </summary>
    public void Start(
        IReadOnlyList<CV_Recommendation> recommendations)
    {
        if (recommendations == null)
            throw new ArgumentNullException(nameof(recommendations));

        _reviewingAll = false;

        _recommendations.Clear();
        _recommendations.AddRange(recommendations);

        _reviewAllItems.Clear();

        _currentRecommendationIndex = -1;

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

    /// <summary>
    /// Interprets a user's response using the Conversation Framework's
    /// user-intent model.
    /// </summary>
    public CV_UserIntent InterpretUserInput(
        string input)
    {
        return _userIntent.Interpret(input);
    }

    /// <summary>
    /// Indicates that Scout has asked the user whether they want
    /// to open the complete Review All report.
    ///
    /// The user's next affirmative response belongs to this question,
    /// not to the recommendation currently being discussed.
    /// </summary>
    public void BeginReviewAllPrompt()
    {
        _awaitingReviewAllApproval = true;
    }

    /// <summary>
    /// Clears the pending Review All question.
    /// </summary>
    public void ClearReviewAllPrompt()
    {
        _awaitingReviewAllApproval = false;
    }

    /// <summary>
    /// Creates an action request for the recommendation currently being
    /// discussed when the user's input represents approval of that
    /// recommendation's next-step action.
    ///
    /// The Conversation Framework does not execute the action.
    /// It creates the generic request that the appropriate domain Expert
    /// can consume.
    /// </summary>
    public CV_ActionRequest? CreateActionRequest(
    string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return null;

        // ---------------------------------------------------------
        // First check whether the user selected one of the options
        // returned by the previous action.
        // ---------------------------------------------------------

        foreach (CV_ActionOption option in _pendingActionOptions)
        {
            if (string.Equals(
                userInput.Trim(),
                option.Id,
                StringComparison.OrdinalIgnoreCase))
            {
                CV_Recommendation? recommendation =
                    _currentTopic.Recommendation;

                if (recommendation == null)
                    return null;

                return new CV_ActionRequest
                {
                    RecommendationId = recommendation.Id,
                    ActionId = option.ActionId,
                    UserInput = userInput,
                    OptionId = option.Id,
                    ContextId = option.ContextId
                };
            }
        }

        // ---------------------------------------------------------
        // Normal conversational action handling.
        // ---------------------------------------------------------

        CV_Recommendation? currentRecommendation =
            _currentTopic.Recommendation;

        if (currentRecommendation == null ||
            !currentRecommendation.HasAction)
        {
            return null;
        }

        CV_UserIntent intent =
            _userIntent.Interpret(userInput);

        if (intent.Type != CV_UserIntentType.Approve &&
            intent.Type != CV_UserIntentType.Research)
        {
            return null;
        }

        return new CV_ActionRequest
        {
            RecommendationId = currentRecommendation.Id,
            ActionId = currentRecommendation.ActionId,
            UserInput = userInput
        };
    }

    public CV_ActionRequest? CreateActionRequest(
    CV_Recommendation recommendation)
    {
        if (recommendation == null)
            return null;

        if (string.IsNullOrWhiteSpace(recommendation.ActionId))
            return null;

        return new CV_ActionRequest
        {
            ActionId = recommendation.ActionId
        };
    }



    /// <summary>
    /// Loads the authoritative recommendations without selecting
    /// an initial conversation topic.
    ///
    /// This is used by Review All, where the complete report must
    /// open with no recommendation selected.
    ///
    /// Normal conversation continues to use Start(), which may
    /// select an initial recommendation.
    /// </summary>
    public void LoadRecommendations(
        IReadOnlyList<CV_Recommendation> recommendations)
    {
        if (recommendations == null)
            throw new ArgumentNullException(nameof(recommendations));

        _reviewingAll = false;

        _recommendations.Clear();
        _recommendations.AddRange(recommendations);

        _reviewAllItems.Clear();

        _currentRecommendationIndex = -1;
        _currentTopic.Clear();
    }
    //---------------------------------------------------------
    // Review All
    //---------------------------------------------------------

    /// <summary>
    /// Builds the complete Review All report.
    ///
    /// IMPORTANT:
    ///
    /// Review All is no longer a sequential conversation.
    ///
    /// It creates one CV_ReviewAllItem for EVERY recommendation.
    ///
    /// Scout remains available beside the report and can discuss any
    /// individual finding when the user asks.
    /// </summary>
    public void ReviewAll()
    {
        _reviewingAll = true;

        if (_recommendations.Count == 0)
        {
            _currentRecommendationIndex = -1;
            _currentTopic.Clear();
            return;
        }



        //---------------------------------------------------------
        // Review All displays the complete report.
        //
        // IMPORTANT:
        // This method does NOT select a recommendation.
        //
        // The automatic selection of recommendation #1 was a
        // temporary test bridge used while we were troubleshooting
        // whether the Review All report would open.
        //
        // The report now opens successfully, so that bridge is
        // intentionally removed.
        //
        // The user selects a recommendation from the report.
        // That selection is then handled by DiscussRecommendation().
        //---------------------------------------------------------

        _currentRecommendationIndex = -1;
        _currentTopic.Clear();

        //---------------------------------------------------------
        // Build the complete report.
        //
        // Nothing is selected.
        // Nothing is discarded.
        // Every recommendation becomes a report item.
        //---------------------------------------------------------

        foreach (CV_Recommendation recommendation in _recommendations)
        {
            _reviewAllItems.Add(
                new CV_ReviewAllItem(recommendation));
        }
    }

    /// <summary>
    /// Advances Review All to the next recommendation.
    /// </summary>
    public bool AdvanceReviewAll()
    {
        if (!_reviewingAll)
            return false;

        if (_currentRecommendationIndex < 0)
            return false;

        int nextIndex =
            _currentRecommendationIndex + 1;

        if (nextIndex >= _recommendations.Count)
        {
            _currentRecommendationIndex = -1;
            _currentTopic.Clear();
            return false;
        }

        _currentRecommendationIndex = nextIndex;

        CV_Recommendation nextRecommendation =
            _recommendations[_currentRecommendationIndex];

        _currentTopic.Begin(nextRecommendation);

        return true;
    }


    //---------------------------------------------------------
    // Discuss Recommendation
    //---------------------------------------------------------

    /// <summary>
    /// Begins a normal Scout conversation about a specific recommendation.
    ///
    /// Review All remains available. Selecting an item from the report
    /// simply moves Scout's conversational focus to that finding.
    /// </summary>
    public CV_ConversationMessage? DiscussRecommendation(
    CV_Recommendation recommendation)
    {
        if (recommendation == null)
            throw new ArgumentNullException(nameof(recommendation));

        int index =
            _recommendations.IndexOf(recommendation);

        if (index >= 0)
        {
            _currentRecommendationIndex = index;
        }

        _currentTopic.Begin(recommendation);

        // Let the Conversation Planner decide what Scout should
        // actually say about the newly selected recommendation.
        CV_ConversationMessage? message =
            _planner.BuildNextMessage(recommendation);

        
        return message;
    }

    //---------------------------------------------------------
    // Review Next
    //---------------------------------------------------------

    /// <summary>
    /// LEGACY / MIGRATION SUPPORT
    ///
    /// The old Review All implementation advanced through recommendations
    /// sequentially.
    ///
    /// Review All no longer uses this behavior.
    ///
    /// This method remains temporarily so existing callers do not break
    /// while the old presentation path is removed.
    /// </summary>
    public bool ReviewNext()
    {
        if (!_reviewingAll)
            return false;

        if (_recommendations.Count == 0)
            return false;

        int nextIndex =
            _currentRecommendationIndex + 1;

        if (nextIndex >= _recommendations.Count)
            return false;

        _currentRecommendationIndex = nextIndex;

        BeginCurrentRecommendation();

        return true;
    }

    //---------------------------------------------------------
    // Finish Review All
    //---------------------------------------------------------

    /// <summary>
    /// Ends Review All mode.
    ///
    /// The report itself is cleared because it belongs to the current
    /// Review All session.
    /// </summary>
    public void FinishReviewAll()
    {
        _reviewingAll = false;

        _currentRecommendationIndex = -1;

        _reviewAllItems.Clear();

        _currentTopic.Clear();
    }

    //---------------------------------------------------------
    // Internal Helpers
    //---------------------------------------------------------

    /// <summary>
    /// Begins the recommendation at the current index.
    ///
    /// Retained temporarily for migration compatibility with the old
    /// sequential Review All path.
    /// </summary>
    private void BeginCurrentRecommendation()
    {
        if (_currentRecommendationIndex < 0 ||
            _currentRecommendationIndex >= _recommendations.Count)
        {
            _currentTopic.Clear();
            return;
        }

        CV_Recommendation currentRecommendation =
            _recommendations[_currentRecommendationIndex];

        _currentTopic.Begin(currentRecommendation);
    }
}