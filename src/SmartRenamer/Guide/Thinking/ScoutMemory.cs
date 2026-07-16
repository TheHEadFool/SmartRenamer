using SmartRenamer.Capabilities.TextReplacement;
using System.Collections.Generic;

namespace SmartRenamer.Guide.Thinking
{
    /// <summary>
    /// Stores everything Scout has learned during
    /// the current conversation.
    /// </summary>
    public class ScoutMemory
    {
        //-------------------------------------------------
        // Conversation
        //-------------------------------------------------

        public List<string> UserAnswers { get; } = new();

        //-------------------------------------------------
        // Goal
        //-------------------------------------------------

        public string? Goal { get; set; }

        public bool GoalKnown =>
            !string.IsNullOrWhiteSpace(Goal);

        //-------------------------------------------------
        // Selected Capability
        //-------------------------------------------------

        public string? Capability { get; set; }

        //-------------------------------------------------
        // Capability Data
        //-------------------------------------------------

        public TextReplacementRequest? TextReplacement { get; set; }

        //-------------------------------------------------
        // Conversation Memory
        //-------------------------------------------------

        public void Remember(string answer)
        {
            UserAnswers.Add(answer);
        }
    }
}