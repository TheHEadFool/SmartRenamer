namespace SmartRenamer.Guide
{
    /// <summary>
    /// Represents where Scout currently is
    /// in the conversation with the user.
    /// </summary>
    public enum ConversationStage
    {
        /// <summary>
        /// Scout introduces itself.
        /// </summary>
        Greeting,

        /// <summary>
        /// Scout learns what success looks like.
        /// </summary>
        DiscoverGoal,

        /// <summary>
        /// Scout asks the user to choose
        /// the folder to investigate.
        /// </summary>
        ChooseFolder,

        /// <summary>
        /// Scout determines whether it should
        /// protect the originals by creating
        /// an organized copy.
        /// </summary>
        ProtectOriginals,

        /// <summary>
        /// Scout learns how the user wants the
        /// files organized.
        /// </summary>
        OrganizationStrategy,

        /// <summary>
        /// Scout learns whether filenames
        /// should change.
        /// </summary>
        NamingStrategy,

        /// <summary>
        /// Scout presents its proposed plan.
        /// </summary>
        ReviewPlan,

        /// <summary>
        /// Scout and the user are discussing
        /// the rename preview. The user can
        /// refine the proposed changes before
        /// anything is renamed.
        /// </summary>
        PreviewConversation,

        /// <summary>
        /// Scout is ready to execute.
        /// </summary>
        Ready
    }
}