using System.Collections.Generic;

namespace SmartRenamer.Guide.Models
{
    /// <summary>
    /// A question Scout asks when it cannot safely
    /// decide something automatically.
    /// </summary>
    public class IntentQuestion
    {
        /// <summary>
        /// What Scout asks the user.
        /// </summary>
        public string Question { get; set; } = "";

        /// <summary>
        /// Possible answers.
        /// </summary>
        public List<string> Choices { get; set; } = new();

        /// <summary>
        /// User's selected answer.
        /// Empty until chosen.
        /// </summary>
        public string SelectedChoice { get; set; } = "";
    }
}