namespace SmartRenamer.Models
{
    public enum ObservationSeverity
    {
        Information,
        Suggestion,
        Warning
    }

    public class ProjectObservation
    {
        public string Title { get; set; } = "";

        public string Description { get; set; } = "";

        public ObservationSeverity Severity { get; set; }
    }
}