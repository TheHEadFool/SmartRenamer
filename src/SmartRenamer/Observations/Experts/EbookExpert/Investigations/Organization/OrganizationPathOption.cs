using Scout.Observations.Experts.EbookExpert.Investigations.Organization;
using System;
using System.Collections.Generic;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization
{
    /// <summary>
    /// Describes one collection-level organization path that Scout can present
    /// to the user.
    ///
    /// This is a description of a possible organization policy.
    /// It is NOT the user's selected OrganizationOptions and it performs
    /// no planning, filesystem work, or UI work.
    ///
    /// Example:
    ///
    ///     Author → Series → Title
    ///
    /// becomes:
    ///
    ///     FolderLevels:
    ///         Author
    ///         Series
    ///
    ///     FileOrderBy:
    ///         Title
    ///
    /// The final user choice will eventually be translated into
    /// OrganizationOptions.
    /// </summary>
    internal sealed class OrganizationPathOption
    {
        /// <summary>
        /// Stable identifier for this selectable organization path.
        ///
        /// This is an internal domain identifier, not an ActionId.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Human-readable name for the path.
        ///
        /// Example:
        ///     "Author → Series → Title"
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Short explanation of what this organization path means.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Folder dimensions used to construct the destination hierarchy.
        ///
        /// Example:
        ///     Author, Series
        ///
        /// does not include Title because Title is normally the file-order /
        /// filename dimension.
        /// </summary>
        public IReadOnlyList<OrganizationDimension> FolderLevels { get; }

        /// <summary>
        /// Dimension used to order files within the resulting folder.
        /// </summary>
        public OrganizationDimension FileOrderBy { get; }

        /// <summary>
        /// Direction in which files are ordered.
        ///
        /// Organization paths currently default to ascending order.
        /// </summary>
        public OrganizationSortDirection FileOrderDirection { get; }

        public OrganizationPathOption(
            string id,
            string label,
            string description,
            IReadOnlyList<OrganizationDimension> folderLevels,
            OrganizationDimension fileOrderBy,
            OrganizationSortDirection fileOrderDirection =
                OrganizationSortDirection.Ascending)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            ArgumentException.ThrowIfNullOrWhiteSpace(label);
            ArgumentException.ThrowIfNullOrWhiteSpace(description);
            ArgumentNullException.ThrowIfNull(folderLevels);

            if (folderLevels.Count == 0)
            {
                throw new ArgumentException(
                    "An organization path must contain at least one folder level.",
                    nameof(folderLevels));
            }

            Id = id;
            Label = label;
            Description = description;
            FolderLevels =
                new List<OrganizationDimension>(
                folderLevels).AsReadOnly();

            FileOrderBy = fileOrderBy;
            FileOrderDirection = fileOrderDirection;
        }
    }
}