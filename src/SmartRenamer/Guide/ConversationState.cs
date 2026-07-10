using System.Collections.Generic;

namespace SmartRenamer.Guide
{
    public class ConversationState
    {
        public ProjectGoal Goal { get; } = new();

        public Understanding Understanding { get; } = new();

        public List<string> ConversationHistory { get; }
            = new();
    }
}