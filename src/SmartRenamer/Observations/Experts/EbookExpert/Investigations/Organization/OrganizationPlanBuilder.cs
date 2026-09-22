using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Scout.Observations.Experts.EbookExpert.Investigations.Organization;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization
{
    /// <summary>
    /// Builds the collection-level organization routing plan from the
    /// already-observed ebook snapshots and the user's organization choices.
    ///
    /// This class performs no filesystem work.
    /// </summary>
    internal sealed class OrganizationPlanBuilder
    {
        public OrganizationPlan Build(OrganizationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            OrganizationPlan plan = new();

            if (!context.Options.IsConfigured)
                return plan;

            List<OrganizationBook> books =
                context.Books
                    .Where(book => book != null)
                    .OrderBy(book => book.OriginalPath, StringComparer.OrdinalIgnoreCase)
                    .ToList();

            List<PlannedBook> plannedBooks = new();

            foreach (OrganizationBook book in books)
            {
                string relativeFolder = BuildRelativeFolder(
                    book,
                    context.Options.FolderLevels);

                string sortValue = GetDimensionValue(
                    book,
                    context.Options.FileOrderBy);

                plannedBooks.Add(
                    new PlannedBook(
                        book,
                        relativeFolder,
                        sortValue));
            }

            IEnumerable<IGrouping<string, PlannedBook>> groups =
                plannedBooks.GroupBy(
                    item => item.RelativeFolder,
                    StringComparer.OrdinalIgnoreCase);

            foreach (IGrouping<string, PlannedBook> group in groups)
            {
                IEnumerable<PlannedBook> ordered =
                    context.Options.FileOrderDirection ==
                    OrganizationSortDirection.Descending
                        ? group
                            .OrderByDescending(
                                item => item.SortValue,
                                StringComparer.OrdinalIgnoreCase)
                            .ThenBy(
                                item => item.Book.Title,
                                StringComparer.OrdinalIgnoreCase)
                            .ThenBy(
                                item => item.Book.OriginalPath,
                                StringComparer.OrdinalIgnoreCase)
                        : group
                            .OrderBy(
                                item => item.SortValue,
                                StringComparer.OrdinalIgnoreCase)
                            .ThenBy(
                                item => item.Book.Title,
                                StringComparer.OrdinalIgnoreCase)
                            .ThenBy(
                                item => item.Book.OriginalPath,
                                StringComparer.OrdinalIgnoreCase);

                int position = 1;

                foreach (PlannedBook item in ordered)
                {
                    string extension =
                        Path.GetExtension(item.Book.WorkingPath);

                    if (string.IsNullOrWhiteSpace(extension))
                        extension = ".epub";

                    string title = SanitizeFileName(
                        string.IsNullOrWhiteSpace(item.Book.Title)
                            ? Path.GetFileNameWithoutExtension(item.Book.WorkingPath)
                            : item.Book.Title);

                    plan.Entries.Add(
                        new OrganizationPlanEntry
                        {
                            OriginalPath = item.Book.OriginalPath,
                            RelativeDestinationFolder = item.RelativeFolder,
                            DestinationFileName =
                                $"{position:00} - {title}{extension}"
                        });

                    position++;
                }
            }

            return plan;
        }

        private static string BuildRelativeFolder(
            OrganizationBook book,
            IReadOnlyList<OrganizationDimension> dimensions)
        {
            List<string> parts = new();

            foreach (OrganizationDimension dimension in dimensions)
            {
                string value = SanitizeFolderName(
                    GetDimensionValue(book, dimension));

                if (string.IsNullOrWhiteSpace(value))
                    value = "Unknown";

                parts.Add(value);
            }

            return string.Join(
                Path.DirectorySeparatorChar,
                parts);
        }

        private static string GetDimensionValue(
            OrganizationBook book,
            OrganizationDimension dimension)
        {
            return dimension switch
            {
                OrganizationDimension.Title => book.Title,
                OrganizationDimension.Author => book.Author,
                OrganizationDimension.Series => book.Series,
                OrganizationDimension.Publisher => book.Publisher,
                OrganizationDimension.ISBN => book.Isbn,
                OrganizationDimension.Language => book.Language,
                OrganizationDimension.PublicationYear => "Unknown",
                OrganizationDimension.PublicationDate => "Unknown",
                _ => string.Empty
            } ?? string.Empty;
        }

        private static string SanitizeFolderName(string value)
        {
            return SanitizePathPart(value);
        }

        private static string SanitizeFileName(string value)
        {
            return SanitizePathPart(value);
        }

        private static string SanitizePathPart(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown";

            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            HashSet<char> invalid = new(invalidCharacters);

            string cleaned =
                new(value.Trim().Select(
                    character => invalid.Contains(character)
                        ? '_'
                        : character).ToArray());

            cleaned = cleaned.Trim().TrimEnd('.', ' ');

            return string.IsNullOrWhiteSpace(cleaned)
                ? "Unknown"
                : cleaned;
        }

        private sealed record PlannedBook(
            OrganizationBook Book,
            string RelativeFolder,
            string SortValue);
    }
}