namespace SmartRenamer.Guide
{
    public class ProjectGoal
    {
        public string Description { get; set; } = "";

        public string DesiredOutcome { get; set; } = "";

        public bool IsComplete =>
            !string.IsNullOrWhiteSpace(Description) &&
            !string.IsNullOrWhiteSpace(DesiredOutcome);
    }
}