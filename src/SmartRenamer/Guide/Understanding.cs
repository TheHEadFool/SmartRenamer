/******************************************************************************
 * Understanding
 *
 * Scout Design Language (SDL-001)
 *
 * PURPOSE
 * -------
 * Represents everything the Guide currently understands about the user's
 * project.
 *
 * Understanding grows as the conversation progresses. Rather than storing
 * individual answers, it measures how complete Scout's understanding is before
 * making recommendations.
 *
 * Scout should never recommend significant actions until its understanding is
 * sufficiently complete.
 ******************************************************************************/

namespace SmartRenamer.Guide
{
    public class Understanding
    {
        /// <summary>
        /// How well Scout understands the user's goal.
        /// </summary>
        public int GoalConfidence { get; set; }

        /// <summary>
        /// Confidence in the selected source.
        /// </summary>
        public int SourceConfidence { get; set; }

        /// <summary>
        /// Confidence in the destination.
        /// </summary>
        public int DestinationConfidence { get; set; }

        /// <summary>
        /// Confidence in the selected naming strategy.
        /// </summary>
        public int NamingConfidence { get; set; }

        /// <summary>
        /// Confidence that the proposed workflow is safe.
        /// </summary>
        public int SafetyConfidence { get; set; }

        /// <summary>
        /// Confidence that Scout understands the user's intent.
        /// </summary>
        public int IntentConfidence { get; set; }

        /// <summary>
        /// Overall understanding expressed as a percentage.
        /// </summary>
        public int OverallConfidence =>
            (GoalConfidence +
             SourceConfidence +
             DestinationConfidence +
             NamingConfidence +
             SafetyConfidence +
             IntentConfidence) / 6;

        /// <summary>
        /// True when Scout has enough understanding to make recommendations.
        /// </summary>
        public bool IsReadyToRecommend =>
            OverallConfidence >= 80;

        /// <summary>
        /// True when Scout has a solid understanding of the project.
        /// </summary>
        public bool IsWellUnderstood =>
            OverallConfidence >= 95;
    }
}