using Scout.Observations;
using SmartRenamer.Models;
using SmartRenamer.Observations;
using System.Collections.Generic;

namespace Scout.Observations.Conversation
{
    /// =========================================================================
    /// CV_ConversationPlanner
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Guide the expedition."
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Coordinates Scout's conversation with the user.
    ///
    /// The ConversationPlanner is the decision-making layer of Scout's
    /// Conversation Framework.
    ///
    /// Experts determine what Scout knows.
    /// The ConversationPlanner determines what Scout should communicate next.
    ///
    /// -------------------------------------------------------------------------
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Receive the current Expert findings.
    /// • Maintain the current recommendation/topic.
    /// • Consider what the user has already been told.
    /// • Determine what information is most valuable to communicate next.
    /// • Preserve critical information when conversation space is limited.
    /// • Avoid repeating information already communicated.
    /// • Respond to user choices and intent.
    /// • Coordinate the conversational state of the current expedition.
    ///
    /// -------------------------------------------------------------------------
    /// Important Design Principle
    /// -------------------------------------------------------------------------
    /// The Planner does NOT simply forward every piece of information supplied
    /// by an Expert.
    ///
    /// Expert findings may contain more information than the conversation
    /// should present at one time.
    ///
    /// The Planner is responsible for deciding what belongs in the next
    /// conversational response.
    ///
    /// -------------------------------------------------------------------------
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Analyze files.
    /// • Know anything about EPUBs, music, documents, or other domains.
    /// • Render WPF controls.
    /// • Manipulate XAML.
    /// • Decide how the ConversationPanel visually displays messages.
    /// • Perform file operations.
    ///
    /// Those responsibilities belong to Experts and the User Interface.
    ///
    /// -------------------------------------------------------------------------
    /// Relationship to the Scout architecture
    /// -------------------------------------------------------------------------
    ///
    ///     Observation Experts
    ///             ↓
    ///       ExpertFindings
    ///             ↓
    ///     Conversation Framework
    ///             ↓
    ///     CV_ConversationPlanner
    ///             ↓
    ///     Conversation Message
    ///             ↓
    ///        User Interface
    ///
    /// The Planner is deliberately domain-independent.
    ///
    /// It can guide conversations about ebooks, music, documents, photos,
    /// recipes, or future Experts without knowing the domain itself.
    ///
    /// =========================================================================
    public sealed class CV_ConversationPlanner
    {
        //---------------------------------------------------------
        // Current expedition data
        //---------------------------------------------------------

        /// <summary>
        /// The findings currently available to Scout.
        /// </summary>
        private IReadOnlyList<ExpertFinding> _findings =
            new List<ExpertFinding>();

        //---------------------------------------------------------
        // Current conversational focus
        //---------------------------------------------------------

        /// <summary>
        /// The recommendation currently being discussed.
        ///
        /// This remains intentionally nullable because Review All and other
        /// navigation states may present recommendations without selecting
        /// one for conversation.
        /// </summary>
        private CV_Recommendation? _currentRecommendation;

        //---------------------------------------------------------
        // Conversation history
        //---------------------------------------------------------

        /// <summary>
        /// Records the conversational information that has already been
        /// presented during the current expedition.
        ///
        /// This will eventually allow the Planner to avoid repeating
        /// information unnecessarily.
        /// </summary>
        private readonly List<string> _conversationHistory = new();

        //---------------------------------------------------------
        // Public state
        //---------------------------------------------------------

        /// <summary>
        /// Gets the findings currently available to the Planner.
        /// </summary>
        public IReadOnlyList<ExpertFinding> Findings =>
            _findings;

        /// <summary>
        /// Gets the recommendation currently being discussed.
        /// </summary>
        public CV_Recommendation? CurrentRecommendation =>
            _currentRecommendation;

        /// <summary>
        /// Gets the information already presented during this expedition.
        /// </summary>
        public IReadOnlyList<string> ConversationHistory =>
            _conversationHistory;

        //---------------------------------------------------------
        // Initial framework entry point
        //---------------------------------------------------------

        /// <summary>
        /// Begins a conversational expedition using the findings supplied
        /// by the Observation Framework.
        ///
        /// This method currently establishes Planner state only.
        ///
        /// Recommendation selection and message generation will be added
        /// only after the required recommendation contract has been verified.
        /// </summary>
        public void BeginExpedition(
            IReadOnlyList<ExpertFinding> findings)
        {
            _findings =
                findings ??
                new List<ExpertFinding>();

            _currentRecommendation = null;

            _conversationHistory.Clear();
        }

        //---------------------------------------------------------
        // Build the next conversational message
        //---------------------------------------------------------

        /// <summary>
        /// Builds one concise conversational message from the current
        /// recommendation.
        ///
        /// The Planner deliberately creates ONE conversational unit rather
        /// than forwarding every piece of recommendation data separately.
        /// This establishes the foundation for Scout's future ability to
        /// decide what information is important enough to communicate.
        ///
        /// The Planner does not render the message or send it to the UI.
        /// It only decides what the next conversational message should be.
        /// </summary>
        public CV_ConversationMessage? BuildNextMessage(
    CV_Recommendation recommendation)
        {
            if (recommendation == null)
                return null;

            _currentRecommendation = recommendation;

            string safety =
                recommendation.SafetyMessage?.Trim()
                ?? string.Empty;

            string question =
                recommendation.Question?.Trim()
                ?? string.Empty;

            string reason =
                recommendation.Reason?.Trim()
                ?? string.Empty;

            string title =
                recommendation.Title?.Trim()
                ?? string.Empty;

            // -------------------------------------------------------------
            // Conversation priority
            //
            // 1. Critical safety information must never be lost.
            // 2. The user's next decision/question is the primary purpose
            //    of the conversational message.
            // 3. Reason/context is included only when it adds useful context.
            // 4. Supporting detail is intentionally deferred.
            // -------------------------------------------------------------

            List<string> parts = new();

            if (!string.IsNullOrWhiteSpace(safety))
            {
                parts.Add(safety);
            }

            if (!string.IsNullOrWhiteSpace(question))
            {
                // When there is a question, keep the conversational focus
                // on the user's next decision.
                if (!string.IsNullOrWhiteSpace(safety))
                {
                    parts.Add(question);
                }
                else if (!string.IsNullOrWhiteSpace(reason))
                {
                    parts.Add(reason);
                    parts.Add(question);
                }
                else
                {
                    parts.Add(question);
                }
            }
            else if (!string.IsNullOrWhiteSpace(reason))
            {
                parts.Add(reason);
            }
            else if (!string.IsNullOrWhiteSpace(title))
            {
                parts.Add($"I found something worth looking at: {title}.");
            }

            if (parts.Count == 0)
                return null;

            string text = string.Join(" ", parts);

            CV_ConversationMessage message =
                new()
                {
                    Role = CV_MessageRole.Scout,
                    Text = text,
                    RecommendationId = recommendation.Id,
                    IsCritical =
                        !string.IsNullOrWhiteSpace(safety),
                    Sequence =
                        _conversationHistory.Count
                };

            // Preserve exactly what was communicated so future Planner
            // decisions can avoid unnecessary repetition.
            _conversationHistory.Add(text);

            return message;
        }

    }
}