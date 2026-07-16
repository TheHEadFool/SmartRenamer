namespace SmartRenamer.Guide.Thinking
{
    /// <summary>
    /// Represents one conclusion Scout has reached.
    /// Thoughts are created from observations and
    /// eventually become conversation.
    /// </summary>
    public class ScoutThought
    {
        /// <summary>
        /// Internal reasoning.
        /// Not shown directly to the user.
        /// </summary>
        public string Thought { get; set; } = "";

        /// <summary>
        /// How important this thought is.
        /// Higher numbers are considered first.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Whether Scout still needs to ask the
        /// user about this thought.
        /// </summary>
        public bool NeedsUserInput { get; set; }

        /// <summary>
        /// The question Scout would ask if more
        /// information is required.
        /// </summary>
        public string Question { get; set; } = "";
    }
}