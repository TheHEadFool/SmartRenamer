namespace SmartRenamer.Services.Naming
{
    public class NamingSuggestion
    {
        public string ProposedName { get; set; } = "";

        public string Reason { get; set; } = "";

        public SuggestionConfidence Confidence { get; set; }

        public bool RequiresFriendConfirmation { get; set; }
    }
}