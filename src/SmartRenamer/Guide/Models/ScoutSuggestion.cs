using System;

namespace SmartRenamer.Guide.Models
{
    /******************************************************************************
     * ScoutSuggestion
     *
     * Represents an action Scout recommends.
     *
     * Suggestions are optional.
     *
     * Scout never commands.
     * Scout recommends.
     ******************************************************************************/

    public class ScoutSuggestion
    {
        /// <summary>
        /// Title displayed to the user.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Short explanation.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the suggestion is currently available.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Optional payload associated with the suggestion.
        /// </summary>
        public object? Payload { get; set; }

        public override string ToString()
        {
            return Title;
        }
    }
}