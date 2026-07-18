using System;
using System.Collections.Generic;
using System.Linq;
using SmartRenamer.Capabilities.TextReplacement;

namespace SmartRenamer.Guide.Thinking
{
    /// <summary>
    /// Scout's conversation engine.
    /// Responsible for understanding the user's intent
    /// and deciding what Scout should ask next.
    /// </summary>
    public class ScoutConversationEngine
    {
        private readonly ScoutMemory memory = new();

        private readonly TextReplacementInterpreter interpreter = new();

        private static readonly string[] approveWords =
        {
            "approve",
            "approved",
            "approval",
            "yes",
            "y",
            "ok",
            "okay",
            "looks good",
            "looks great",
            "looks fine",
            "go ahead",
            "rename",
            "rename them",
            "rename it",
            "apply",
            "continue",
            "proceed",
            "do it"
        };

        private static readonly string[] cancelWords =
        {
            "cancel",
            "stop",
            "never mind",
            "nevermind",
            "forget it"
        };

        private static readonly string[] refineWords =
        {
            "leave",
            "except",
            "only",
            "keep",
            "don't",
            "do not"
        };

        private static readonly string[] helpWords =
{
    "help",
    "why",
    "explain",
    "what do you recommend",
    "can you recommend",
    "what should i do",
    "not sure",
    "i'm not sure",
    "im not sure",
    "maybe",
    "i don't know",
    "i dont know"
};

        private static readonly string[] beginWords =
        {
            "go",
            "start",
            "begin",
            "continue",
            "ready",
            "i'm ready",
            "im ready",
            "let's go",
            "lets go",
            "go ahead",
            "analyze",
            "analyse",
            "look at the folder",
            "investigate"
        };

        public ScoutQuestion? GetNextQuestion(
            List<ScoutThought> thoughts)
        {
            if (!memory.GoalKnown)
            {
                return new ScoutQuestion
                {
                    Id = "goal",
                    Text =
                        "Before I investigate anything, what would you like to accomplish?"
                };
            }

            if (!memory.InvestigationComplete)
            {
                return new ScoutQuestion
                {
                    Id = "folder",
                    Text =
                        "I'll open the folder browser so you can choose the folder you'd like me to investigate."
                };
            }

            return new ScoutQuestion
            {
                Id = "preview",
                Text =
                    "I've prepared the preview. If everything looks correct, type 'approve' and I'll rename the files. Otherwise, tell me what you'd like to change."
            };
        }

        public void ProcessAnswer(string answer)
        {
            memory.Remember(answer);

            if (!memory.GoalKnown)
            {
                memory.Goal = answer;
            }

            if (interpreter.TryInterpret(
                answer,
                out TextReplacementRequest request))
            {
                memory.Capability = "TextReplacement";
                memory.TextReplacement = request;
            }
        }

        public void InvestigationCompleted()
        {
            memory.InvestigationComplete = true;
        }

        public ConversationIntent GetIntent(string answer)
        {
            if (ContainsPhrase(answer, beginWords))
                return ConversationIntent.Begin;

            if (ContainsPhrase(answer, approveWords))
                return ConversationIntent.Approve;

            if (ContainsPhrase(answer, refineWords))
                return ConversationIntent.Refine;

            if (ContainsPhrase(answer, helpWords))
                return ConversationIntent.Help;

            if (ContainsPhrase(answer, cancelWords))
                return ConversationIntent.Cancel;

            return ConversationIntent.Unknown;
        }

        private static bool ContainsPhrase(
            string answer,
            IEnumerable<string> phrases)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return false;

            string text = answer.Trim().ToLowerInvariant();

            return phrases.Any(p =>
                text.Contains(p, StringComparison.OrdinalIgnoreCase));
        }
    }
}