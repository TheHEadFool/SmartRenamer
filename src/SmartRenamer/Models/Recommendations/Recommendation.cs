using System;

namespace SmartRenamer.Models.Recommendations
{
    /// <summary>
    /// Represents a recommendation Scout has made for the current project.
    /// </summary>
    public class Recommendation
    {
        /// <summary>
        /// Unique identifier.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// Observation this recommendation belongs to.
        /// </summary>
        public Guid ObservationId { get; set; }

        /// <summary>
        /// Stable action identifier used by Scout.
        /// Leave blank for informational recommendations.
        /// </summary>
        public string ActionId { get; set; } = "";

        /// <summary>
        /// Capability that created the recommendation.
        /// </summary>
        public string Capability { get; set; } = "";

        /// <summary>
        /// Title shown to the user.
        /// </summary>
        public string Title { get; set; } = "";

        /// <summary>
        /// Description Scout speaks.
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Text shown on the action button.
        /// Leave blank if no action is available.
        /// </summary>
        public string ActionText { get; set; } = "";

        /// <summary>
        /// True when this recommendation has an action the user can perform.
        /// </summary>
        public bool HasAction => !string.IsNullOrWhiteSpace(ActionId);

        /// <summary>
        /// Optional icon name.
        /// </summary>
        public string Icon { get; set; } = "";

        /// <summary>
        /// Estimated number of affected files.
        /// </summary>
        public int EstimatedChanges { get; set; }

        /// <summary>
        /// Higher numbers appear first.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Whether the user has selected this recommendation.
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Whether an internet connection is required.
        /// </summary>
        public bool RequiresInternet { get; set; }

        /// <summary>
        /// Whether Scout should ask for confirmation before executing.
        /// </summary>
        public bool RequiresConfirmation { get; set; } = true;
    }
}