using System;

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
        public Guid Id { get; set; }

        public string Title { get; set; } = "";

        public string Description { get; set; } = "";

        public string WhyItMatters { get; set; } = "";

        public ObservationSeverity Severity { get; set; }

        public ObservationPriority Priority { get; set; }
    = ObservationPriority.Medium;

        public bool IsRecommended { get; set; }

        public bool IsSelected { get; set; }

        public string ActionTitle { get; set; } = string.Empty;

        public string ActionDescription { get; set; } = string.Empty;

    }
}