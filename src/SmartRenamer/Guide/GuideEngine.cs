namespace SmartRenamer.Guide
{
    public class GuideEngine
    {
        public ConversationState State { get; } = new();

        public string CurrentMessage => GetCurrentMessage();

        private string GetCurrentMessage()
        {
            if (State.ConversationHistory.Count == 0)
            {
                return "What project are we putting together today?";
            }

            return "Let's keep discovering your project together.";
        }

        public void RecordUserResponse(string response)
        {
            State.ConversationHistory.Add(response);
        }

        public bool ReadyToBuildWorkflow()
        {
            return State.Understanding.OverallConfidence >= 80;
        }
    }
}