using System;
using System.Collections.Generic;
using Scout.Observations.Experts.EbookExpert.Investigations.Organization;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization
{
    /// <summary>
    /// Represents one collection-level Ebook Organization job.
    ///
    /// The job is the orchestration boundary between planning and the
    /// eventual execution of individual organization operations.
    ///
    /// The job retains the observed book snapshots needed to resolve a
    /// plan entry back to its current working representation.
    ///
    /// The job does not materialize a collection of execution operations.
    /// Individual work can therefore be created or processed as needed.
    ///
    /// This class performs no filesystem work.
    /// </summary>
    internal sealed class OrganizationJob
    {
        private readonly Dictionary<string, OrganizationBook> _booksByOriginalPath;

        public OrganizationPlan Plan { get; }

        public OrganizationOptions Options { get; }

        public IReadOnlyList<OrganizationBook> Books { get; }

        public IReadOnlyList<OrganizationPlanEntry> Entries =>
            Plan.Entries;

        public OrganizationJob(
            OrganizationPlan plan,
            OrganizationOptions options,
            IReadOnlyList<OrganizationBook> books)
        {
            Plan =
                plan ??
                throw new ArgumentNullException(nameof(plan));

            Options =
                options ??
                throw new ArgumentNullException(nameof(options));

            Books =
                books ??
                throw new ArgumentNullException(nameof(books));

            _booksByOriginalPath =
                new Dictionary<string, OrganizationBook>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (OrganizationBook book in Books)
            {
                if (book == null ||
                    string.IsNullOrWhiteSpace(book.OriginalPath))
                {
                    continue;
                }

                _booksByOriginalPath[book.OriginalPath] = book;
            }
        }

        /// <summary>
        /// Resolves a planned entry to the observed ebook snapshot that owns
        /// the same stable original identity.
        ///
        /// This is an in-memory identity lookup only. It performs no
        /// filesystem access.
        /// </summary>
        public bool TryGetBook(
            OrganizationPlanEntry entry,
            out OrganizationBook? book)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (string.IsNullOrWhiteSpace(entry.OriginalPath))
            {
                book = null;
                return false;
            }

            return _booksByOriginalPath.TryGetValue(
                entry.OriginalPath,
                out book);
        }
    }
}
