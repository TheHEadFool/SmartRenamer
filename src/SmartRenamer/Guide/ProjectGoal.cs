/******************************************************************************
 * ProjectGoal
 *
 * PURPOSE
 * -------
 * Represents the user's mission for the current Scout session.
 *
 * This is not a list of rename rules. It answers the question:
 *
 *     "What is the user trying to accomplish?"
 *
 * Every recommendation made by Scout should support this goal.
 ******************************************************************************/

using System.Collections.Generic;

namespace SmartRenamer.Guide
{
    public class ProjectGoal
    {
        /// <summary>
        /// Short description of the user's objective.
        /// Example: "Organize my vacation photos."
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// What success looks like when Scout is finished.
        /// </summary>
        public string DesiredOutcome { get; set; } = "";

        /// <summary>
        /// Important constraints or requirements provided by the user.
        /// Example: "Never overwrite existing files."
        /// </summary>
        public List<string> Constraints { get; } = new();

        /// <summary>
        /// Things the user values most while accomplishing the goal.
        /// Example: Safety, Speed, Simplicity.
        /// </summary>
        public List<string> Priorities { get; } = new();

        /// <summary>
        /// Indicates whether Scout has enough information to understand the mission.
        /// </summary>
        public bool IsComplete =>
            !string.IsNullOrWhiteSpace(Description) &&
            !string.IsNullOrWhiteSpace(DesiredOutcome);
    }
}