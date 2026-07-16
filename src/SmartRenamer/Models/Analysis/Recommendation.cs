namespace SmartRenamer.Models.Analysis
{
    /// <summary>
    /// Represents a recommendation produced by the
    /// Intelligence Engine.
    /// Scout explains these recommendations to the user,
    /// but does not create them.
    /// </summary>
    public class Recommendation
    {
        /// <summary>
        /// Optional emoji or icon displayed beside the recommendation.
        /// </summary>
        public string Icon { get; set; } = "";

        /// <summary>
        /// Short user-friendly title.
        /// </summary>
        public string Title { get; set; } = "";

        /// <summary>
        /// Explains why this recommendation is being made.
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// If Scout needs the user's input before continuing,
        /// this is the question Scout should ask.
        /// Leave blank if no question is needed.
        /// </summary>
        public string Question { get; set; } = "";

        /// <summary>
        /// Indicates whether Scout should ask the question
        /// before adding this recommendation to the workflow.
        /// </summary>
        public bool RequiresDecision { get; set; }

        /// <summary>
        /// Higher priority recommendations appear first.
        /// </summary>
        public int Priority { get; set; }
    }
}