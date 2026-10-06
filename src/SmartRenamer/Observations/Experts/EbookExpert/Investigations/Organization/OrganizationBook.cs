namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization
{
    /// <summary>
    /// Represents the per-book information the Ebook Expert's
    /// organization stage may use when planning a destination.
    ///
    /// This is a passive organization-domain snapshot. It does not read,
    /// modify, copy, move, or rename an ebook.
    /// </summary>
    internal sealed class OrganizationBook
    {
        /// <summary>
        /// Stable identity of the protected source ebook.
        /// </summary>
        public string OriginalPath { get; init; } = "";

        /// <summary>
        /// Current working representation available to organization.
        /// </summary>
        public string WorkingPath { get; init; } = "";

        /// <summary>
        /// True when this book is currently eligible for physical organization.
        /// An unresolved repair may remain in the organization snapshot for
        /// tracking while remaining excluded from the final copy plan.
        /// </summary>
        public bool EligibleForOrganization { get; init; } = true;

        /// <summary>
        /// Ebook title.
        /// </summary>
        public string Title { get; init; } = "";

        /// <summary>
        /// Ebook author.
        /// </summary>
        public string Author { get; init; } = "";

        /// <summary>
        /// Ebook series.
        /// </summary>
        public string Series { get; init; } = "";

        /// <summary>
        /// Established position within the series, when the available
        /// metadata/evidence provides one. Organization must never invent
        /// this value merely because several books share a series folder.
        /// </summary>
        public string SeriesNumber { get; init; } = "";

        /// <summary>
        /// Ebook publisher.
        /// </summary>
        public string Publisher { get; init; } = "";

        /// <summary>
        /// Ebook ISBN.
        /// </summary>
        public string Isbn { get; init; } = "";

        /// <summary>
        /// Ebook language.
        /// </summary>
        public string Language { get; init; } = "";
    }
}
