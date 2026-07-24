using System.Collections.Generic;

namespace SmartRenamer.Observations.Insights
{
    /// <summary>
    /// =========================================================================
    /// ExpertInsight
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Transform findings into understanding."
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Represents the synthesized understanding produced by an Expert after
    /// reviewing the findings from one or more Specialists.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Summarize what the Expert learned
    /// • Present supporting evidence
    /// • Record unanswered questions
    /// • Express confidence in the conclusion
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Collect data
    /// • Read files
    /// • Modify the project
    /// • Interact directly with Scout
    ///
    /// Relationship to Scout
    /// -------------------------------------------------------------------------
    /// Building Blocks
    ///        ↓
    /// Specialists
    ///        ↓
    /// Expert Findings
    ///        ↓
    /// Expert Insight
    ///        ↓
    /// Scout Conversation
    /// =========================================================================
    /// </summary>
    public class ExpertInsight
    {
        /// <summary>
        /// A concise summary of the Expert's overall understanding.
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Supporting facts that led to the insight.
        /// </summary>
        public List<string> Evidence { get; } = new();

        /// <summary>
        /// Important questions that remain unanswered.
        /// </summary>
        public List<string> Questions { get; } = new();

        /// <summary>
        /// The Expert's confidence in this insight (0.0 to 1.0).
        /// </summary>
        public double Confidence { get; set; } = 1.0;
    }
}