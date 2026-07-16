namespace SmartRenamer.Models.Settings
{
    /// <summary>
    /// Stores information about the user and
    /// how they prefer to interact with Scout.
    /// </summary>
    public class UserProfile
    {
        /// <summary>
        /// What the user wants Scout to call them.
        /// </summary>
        public string UserName { get; set; } = "";

        /// <summary>
        /// The name of the assistant.
        /// Defaults to Guide.
        /// </summary>
        public string GuideName { get; set; } = "Guide";

        /// <summary>
        /// Has the onboarding conversation completed?
        /// </summary>
        public bool HasCompletedWelcome { get; set; }

        /// <summary>
        /// The user may personalize Guide,
        /// but never has to.
        /// </summary>
        public bool HasCustomGuideName { get; set; }

        /// <summary>
        /// Guide should learn user preferences
        /// over time.
        /// </summary>
        public bool AllowLearning { get; set; } = true;

        /// <summary>
        /// Guide may optionally research unknown
        /// file types online.
        /// </summary>
        public bool AllowOnlineResearch { get; set; } = true;
    }
}