using System.Collections.Generic;

namespace Scout.Observations.Experts.EbookExpert.Investigations.Organization
{
    /// <summary>
    /// Defines the user's collection-level organization choices.
    ///
    /// These settings describe how Scout should build the organized
    /// collection. They are deliberately separate from the source collection
    /// and from temporary working paths used during repair.
    ///
    /// The source collection remains protected. Organization produces
    /// a separate organized representation at the user-selected destination.
    /// </summary>
    public sealed class OrganizationOptions
    {
        /// <summary>
        /// The metadata dimensions the user wants to use as folder levels,
        /// in order from the outermost folder to the innermost folder.
        ///
        /// Examples:
        ///
        ///     Series
        ///
        ///     Author -> Series
        ///
        ///     Series -> Author
        ///
        ///     Author -> PublicationYear -> Series
        ///
        /// The list is intentionally extensible so Scout is not limited
        /// to a fixed number of organization levels.
        /// </summary>
        public List<OrganizationDimension> FolderLevels { get; set; }
            = new();

        /// <summary>
        /// The metadata dimension used when determining the filename order
        /// within an organization folder.
        ///
        /// For example, a Series organization might contain:
        ///
        ///     01 - Born to Run.epub
        ///     02 - Book Two.epub
        ///
        /// while an alphabetical Title organization could use Title here.
        /// </summary>
        public OrganizationDimension FileOrderBy { get; set; }
            = OrganizationDimension.Title;

        /// <summary>
        /// Determines whether files are ordered ascending or descending
        /// by the selected file-order dimension.
        /// </summary>
        public OrganizationSortDirection FileOrderDirection { get; set; }
            = OrganizationSortDirection.Ascending;

        /// <summary>
        /// The root destination selected by the user for the organized
        /// collection.
        ///
        /// This is independent of:
        /// - the source collection location
        /// - an EPUB's original location
        /// - Scout's temporary repair workspace
        ///
        /// The original source remains protected.
        /// </summary>
        public string DestinationRoot { get; set; } = string.Empty;

        /// <summary>
        /// True when Scout has enough organization configuration to begin
        /// planning destinations for individual items.
        ///
        /// This does not authorize filesystem changes. It only indicates
        /// that organization planning has been configured.
        /// </summary>
        public bool IsConfigured =>
            FolderLevels.Count > 0 &&
            !string.IsNullOrWhiteSpace(DestinationRoot);
    }

    /// <summary>
    /// Metadata dimensions Scout may use when organizing an ebook collection.
    ///
    /// A dimension may exist here before the current EPUB metadata reader
    /// supplies it. The organization UI/planner must only offer a dimension
    /// when the current Expert has actually collected the required data.
    /// </summary>
    public enum OrganizationDimension
    {
        Title,
        Author,
        Series,
        Publisher,
        ISBN,
        Language,
        PublicationYear,
        PublicationDate
    }

    /// <summary>
    /// Ordering direction for items within an organization folder.
    /// </summary>
    public enum OrganizationSortDirection
    {
        Ascending,
        Descending
    }
}