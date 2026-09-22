using System;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization
{
    /// <summary>
    /// Executes one planned organization entry.
    ///
    /// This class is deliberately small and stateless.
    ///
    /// It resolves the planned entry through the collection-level
    /// OrganizationJob and delegates the physical copy and verification
    /// to OrganizationCopyOperation.
    ///
    /// It does not:
    /// • modify the original EPUB
    /// • release the working representation
    /// • manage the collection queue
    /// • manage UI
    /// • own concurrency
    /// </summary>
    internal sealed class OrganizationJobExecutor
    {
        private readonly OrganizationCopyOperation _copyOperation = new();

        public OrganizationCopyResult Execute(
            OrganizationJob job,
            OrganizationPlanEntry entry)
        {
            ArgumentNullException.ThrowIfNull(job);
            ArgumentNullException.ThrowIfNull(entry);

            if (!job.TryGetBook(
                    entry,
                    out OrganizationBook? book))
            {
                return OrganizationCopyResult.Failed(
                    "The organization plan entry could not be matched to an observed ebook.");
            }

            if (book == null)
            {
                return OrganizationCopyResult.Failed(
                    "The organization plan entry resolved to no ebook.");
            }

            return _copyOperation.Execute(
                book,
                entry,
                job.Options);
        }
    }
}