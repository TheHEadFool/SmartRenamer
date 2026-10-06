using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

            bool organizeBySeries =
                context.Options.FolderLevels.Contains(
                    OrganizationDimension.Series);

            foreach (IGrouping<string, PlannedBook> group in groups)
            {
                IEnumerable<PlannedBook> ordered;

                if (organizeBySeries)
                {
                    ordered =
                        context.Options.FileOrderDirection ==
                        OrganizationSortDirection.Descending
                            ? group
                                .OrderBy(
                                    item => GetSeriesNumberSortValue(item.Book.SeriesNumber).HasValue ? 0 : 1)
                                .ThenByDescending(
                                    item => GetSeriesNumberSortValue(item.Book.SeriesNumber) ?? decimal.MinValue)
                                .ThenBy(
                                    item => item.Book.Title,
                                    StringComparer.OrdinalIgnoreCase)
                                .ThenBy(
                                    item => item.Book.OriginalPath,
                                    StringComparer.OrdinalIgnoreCase)
                            : group
                                .OrderBy(
                                    item => GetSeriesNumberSortValue(item.Book.SeriesNumber).HasValue ? 0 : 1)
                                .ThenBy(
                                    item => GetSeriesNumberSortValue(item.Book.SeriesNumber) ?? decimal.MaxValue)
                                .ThenBy(
                                    item => item.Book.Title,
                                    StringComparer.OrdinalIgnoreCase)
                                .ThenBy(
                                    item => item.Book.OriginalPath,
                                    StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    ordered =
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
                }

                foreach (PlannedBook item in ordered)
                {
                    string extension =
                        Path.GetExtension(item.Book.WorkingPath);

                    if (string.IsNullOrWhiteSpace(extension))
                        extension = ".epub";

                    string rawTitle =
                        string.IsNullOrWhiteSpace(item.Book.Title)
                            ? Path.GetFileNameWithoutExtension(item.Book.WorkingPath)
                            : item.Book.Title;

                    string title = SanitizeFileName(
                        NormalizeTitleForOrganizationFileName(
                            rawTitle,
                            item.Book.Series,
                            item.Book.SeriesNumber));

                    string filePrefix =
                        organizeBySeries &&
                        TryFormatSeriesNumber(
                            item.Book.SeriesNumber,
                            out string formattedSeriesNumber)
                            ? $"{formattedSeriesNumber} - "
                            : string.Empty;

                    plan.Entries.Add(
                        new OrganizationPlanEntry
                        {
                            OriginalPath = item.Book.OriginalPath,
                            RelativeDestinationFolder = item.RelativeFolder,
                            DestinationFileName =
                                $"{filePrefix}{title}{extension}"
                        });
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
                string rawValue =
                    GetDimensionValue(book, dimension);

                // A selected organization dimension is a policy choice, not
                // a requirement to manufacture a placeholder folder. If this
                // particular book has no usable value for that dimension,
                // remove that dimension from this book's path and preserve
                // the remaining selected hierarchy. For example:
                //
                //     Series → Author → Title
                //
                // becomes Author → Title when Series is genuinely absent.
                //
                // "Unknown" is therefore reserved for a future explicit
                // unresolved state rather than being used to hide missing
                // metadata during organization.
                if (string.IsNullOrWhiteSpace(rawValue))
                    continue;

                string value =
                    SanitizeFolderName(rawValue);

                if (string.IsNullOrWhiteSpace(value))
                    continue;

                parts.Add(value);
            }

            return string.Join(
                Path.DirectorySeparatorChar,
                parts);
        }

        private static decimal? GetSeriesNumberSortValue(
            string value)
        {
            if (decimal.TryParse(
                    value?.Trim(),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out decimal number))
            {
                return number;
            }

            return null;
        }

        private static bool TryFormatSeriesNumber(
            string value,
            out string formatted)
        {
            decimal? number =
                GetSeriesNumberSortValue(value);

            if (!number.HasValue)
            {
                formatted = string.Empty;
                return false;
            }

            formatted =
                number.Value.ToString(
                    "00.##",
                    CultureInfo.InvariantCulture);

            return true;
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


        /// <summary>
        /// Removes a redundant series/series-number prefix from the physical
        /// filename when those values have already been independently
        /// established by the organization model. The series remains in the
        /// folder hierarchy; it is not duplicated in the filename.
        ///
        /// Example:
        ///     SERRAted Edge #02 - Wheels of Fire
        /// becomes:
        ///     Wheels of Fire
        ///
        /// No series name is hard-coded here, and the method refuses to strip
        /// a prefix unless both Series and SeriesNumber are established.
        /// </summary>
        private static string NormalizeTitleForOrganizationFileName(
            string title,
            string series,
            string seriesNumber)
        {
            if (string.IsNullOrWhiteSpace(title) ||
                string.IsNullOrWhiteSpace(series) ||
                string.IsNullOrWhiteSpace(seriesNumber))
            {
                return title;
            }

            decimal? establishedNumber =
                GetSeriesNumberSortValue(seriesNumber);

            if (!establishedNumber.HasValue)
                return title;

            string pattern =
                "^\\s*" +
                Regex.Escape(series.Trim()) +
                "\\s*#\\s*(?<number>\\d+(?:\\.\\d+)?)" +
                "\\s*-\\s*(?<title>.+)$";

            Match match = Regex.Match(
                title.Trim(),
                pattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (!match.Success)
                return title;

            if (!decimal.TryParse(
                    match.Groups["number"].Value,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out decimal titleNumber) ||
                titleNumber != establishedNumber.Value)
            {
                return title;
            }

            string normalizedTitle =
                match.Groups["title"].Value.Trim();

            return string.IsNullOrWhiteSpace(normalizedTitle)
                ? title
                : normalizedTitle;
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