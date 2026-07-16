using System.Collections.Generic;
using System.Linq;
using SmartRenamer.Capabilities.TextReplacement;

namespace SmartRenamer.Guide.Thinking
{
    /// <summary>
    /// Scout's conversation engine.
    /// Determines what Scout should ask next
    /// and learns from Friend's responses.
    /// </summary>
    public class ScoutConversationEngine
    {
        private readonly ScoutMemory memory = new();

        private readonly TextReplacementInterpreter
            interpreter = new();

        public ScoutQuestion? GetNextQuestion(
            List<ScoutThought> thoughts)
        {
            //-------------------------------------------------
            // Scout always begins by learning the goal.
            //-------------------------------------------------

            if (!memory.GoalKnown)
            {
                return new ScoutQuestion
                {
                    Id = "goal",

                    Text =
                        "Before I investigate anything, what would you like to accomplish?"
                };
            }

            //-------------------------------------------------
            // Scout already understands a capability.
            //-------------------------------------------------

            if (!string.IsNullOrWhiteSpace(memory.Capability))
            {
                return new ScoutQuestion
                {
                    Id = "ready",

                    Text =
                        "Great. I understand what you'd like to do. The next step is choosing the folder."
                };
            }

            //-------------------------------------------------
            // Otherwise ask the highest priority question.
            //-------------------------------------------------

            ScoutThought? nextThought =
                thoughts
                .Where(t => t.NeedsUserInput)
                .OrderByDescending(t => t.Priority)
                .FirstOrDefault();

            if (nextThought == null)
                return null;

            return new ScoutQuestion
            {
                Id = "thought",
                Text = nextThought.Question
            };
        }

        public void ProcessAnswer(string answer)
        {
            memory.Remember(answer);

            //-------------------------------------------------
            // First answer becomes Friend's goal.
            //-------------------------------------------------

            if (!memory.GoalKnown)
            {
                memory.Goal = answer;
            }

            //-------------------------------------------------
            // Try to understand a capability.
            //-------------------------------------------------

            if (interpreter.TryInterpret(
                answer,
                out TextReplacementRequest request))
            {
                memory.Capability = "TextReplacement";
                memory.TextReplacement = request;
            }
        }
    }
}