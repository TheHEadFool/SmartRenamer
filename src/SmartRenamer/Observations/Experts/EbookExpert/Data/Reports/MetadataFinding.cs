namespace SmartRenamer.Observations.Experts.EbookExpert.Data.Reports
{
    /// <summary>
    /// Represents a single piece of knowledge discovered during
    /// a metadata investigation.
    /// </summary>
    public class MetadataFinding
    {
        /// <summary>
        /// Short title shown to the user.
        /// </summary>
        public string Title { get; set; } = "";

        /// <summary>
        /// Explains what Scout discovered.
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// Why the discovery matters.
        /// </summary>
        public string WhyItMatters { get; set; } = "";

        /// <summary>
        /// Indicates whether Scout knows how to repair this issue.
        /// </summary>
        public bool CanRepair { get; set; }

        /// <summary>
        /// Question Scout can ask the user.
        /// Leave blank if no question is needed.
        /// </summary>
        public string Question { get; set; } = "";
    }
}