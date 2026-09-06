namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// =========================================================================
    /// E_RepairAuthorization
    /// =========================================================================
    ///
    /// Represents the user's authorization for Scout to automatically handle
    /// qualifying ebook repairs during the current repair expedition.
    ///
    /// This is domain state.
    ///
    /// The Conversation Framework does not know what this authorization means.
    /// The Ebook Expert decides which repairs qualify for automatic handling.
    ///
    /// Authorization is deliberately scoped to the current repair expedition.
    /// It is not a permanent user preference.
    ///
    /// =========================================================================
    /// </summary>
    public sealed class E_RepairAuthorization
    {
        /// <summary>
        /// True when the user has authorized Scout to automatically handle
        /// qualifying repairs during the current repair expedition.
        /// </summary>
        public bool AutomaticallyHandleQualifyingRepairs { get; private set; }

        /// <summary>
        /// Grants authorization for Scout to automatically handle repairs
        /// that the Ebook Expert determines are safe to determine.
        /// </summary>
        public void AuthorizeAutomaticRepairs()
        {
            AutomaticallyHandleQualifyingRepairs = true;
        }

        /// <summary>
        /// Clears automatic-repair authorization.
        ///
        /// This is used when a repair expedition ends and a new processing
        /// cycle begins.
        /// </summary>
        public void Clear()
        {
            AutomaticallyHandleQualifyingRepairs = false;
        }
    }
}