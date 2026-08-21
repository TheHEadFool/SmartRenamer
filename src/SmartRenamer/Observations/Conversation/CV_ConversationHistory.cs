using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace Scout.Observations.Conversation
{
    /// <summary>
    /// =========================================================================
    /// CV_ConversationHistory
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Remember the journey."
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Maintains the conversational history of the current expedition.
    ///
    /// Future Responsibilities
    /// -------------------------------------------------------------------------
    /// • Preserve every conversational exchange.
    /// • Support reviewing previous decisions.
    /// • Provide context for future questions.
    /// • Allow Scout to reference earlier discussions.
    /// • Support future conversation persistence.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Decide what Scout should say.
    /// • Interpret user intent.
    /// • Analyze files.
    ///
    /// Those responsibilities belong to the Conversation Planner,
    /// UserIntent, and the Experts.
    /// =========================================================================
    /// </summary>
    public sealed class CV_ConversationHistory
    {
        private readonly List<string> entries = new();

        /// <summary>
        /// Number of conversational entries currently preserved.
        /// </summary>
        public int Count => entries.Count;

        /// <summary>
        /// Adds a conversational entry to the history.
        /// </summary>
        public void Add(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            entries.Add(text);
        }

        /// <summary>
        /// Provides read-only access to the preserved conversation.
        /// </summary>
        public IReadOnlyList<string> Entries => entries.AsReadOnly();

        /// <summary>
        /// Removes all preserved conversation history.
        /// </summary>
        public void Clear()
        {
            entries.Clear();
        }
    }
}
/// =========================================================================
/// CV_ConversationHistory
/// =========================================================================
///
/// Motto
/// -------------------------------------------------------------------------
/// "Remember the journey."
///
/// Purpose
/// -------------------------------------------------------------------------
/// Maintains the conversational history of the current expedition.
///
/// Future Responsibilities
/// -------------------------------------------------------------------------
/// • Preserve every conversational exchange.
/// • Support reviewing previous decisions.
/// • Provide context for future questions.
/// • Allow Scout to reference earlier discussions.
/// • Support future conversation persistence.
///
/// This class does NOT
/// -------------------------------------------------------------------------
/// • Decide what Scout should say.
/// • Interpret user intent.
/// • Analyze files.
///
/// Those responsibilities belong to the Conversation Planner,
/// UserIntent, and the Experts.
/// =========================================================================