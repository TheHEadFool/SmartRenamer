using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization
{
    /// <summary>
    /// =========================================================================
    /// OrganizationPlan
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Holds the collection-level routing plan produced from the user's
    /// organization choices and the Ebook Expert's organization knowledge.
    ///
    /// Each entry answers:
    ///
    ///     "Where should this particular ebook go?"
    ///
    /// The plan is a map of known books to their planned destinations.
    ///
    /// The destination root is NOT stored here.
    /// It remains in OrganizationOptions because it represents the user's
    /// confirmed destination choice.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Analyze metadata.
    /// • Decide organization rules.
    /// • Ask the user questions.
    /// • Create directories.
    /// • Copy or move files.
    /// • Modify source files.
    /// • Execute an organization job.
    ///
    /// It is only the retained collection-level plan.
    /// =========================================================================
    /// </summary>
    public sealed class OrganizationPlan
    {
        /// <summary>
        /// Individual routing entries for books in the collection.
        ///
        /// Each entry is identified by the book's stable OriginalPath.
        /// </summary>
        public List<OrganizationPlanEntry> Entries { get; } = new();
    }
}