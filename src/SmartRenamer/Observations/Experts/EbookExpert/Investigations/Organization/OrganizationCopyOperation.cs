using Scout.Observations.Experts.EbookExpert.Investigations.Organization;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Organization
{
    /// <summary>
    /// Performs the physical copy for one already-planned ebook and verifies
    /// that the destination contains a readable EPUB.
    ///
    /// This operation copies the current working representation only.
    /// The protected original is never used as the copy source.
    ///
    /// The operation does not release or delete the working copy. Release is
    /// a later lifecycle decision after the Ebook Expert has completed its
    /// processing and the organized representation has been verified.
    /// </summary>
    internal sealed class OrganizationCopyOperation
    {
        public OrganizationCopyResult Execute(
            OrganizationBook book,
            OrganizationPlanEntry entry,
            OrganizationOptions options)
        {
            ArgumentNullException.ThrowIfNull(book);
            ArgumentNullException.ThrowIfNull(entry);
            ArgumentNullException.ThrowIfNull(options);

            if (!entry.IsPlanned)
                return OrganizationCopyResult.Failed(
                    "The organization plan entry is not complete.");

            if (!options.IsConfigured)
                return OrganizationCopyResult.Failed(
                    "Organization has not been configured.");

            if (string.IsNullOrWhiteSpace(book.WorkingPath))
                return OrganizationCopyResult.Failed(
                    "The book has no working representation.");

            if (!File.Exists(book.WorkingPath))
                return OrganizationCopyResult.Failed(
                    $"The working representation was not found: {book.WorkingPath}");

            string destinationRoot = Path.GetFullPath(options.DestinationRoot);
            string destinationFolder = Path.GetFullPath(
                Path.Combine(destinationRoot, entry.RelativeDestinationFolder));
            string destinationPath = Path.GetFullPath(
                Path.Combine(destinationFolder, entry.DestinationFileName));

            string workingPath = Path.GetFullPath(book.WorkingPath);

            if (string.Equals(
                    workingPath,
                    destinationPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                return OrganizationCopyResult.Failed(
                    "The working representation and organization destination are the same file.");
            }

            if (File.Exists(destinationPath))
            {
                return OrganizationCopyResult.Failed(
                    $"The organization destination already exists: {destinationPath}");
            }

            try
            {
                Directory.CreateDirectory(destinationFolder);

                File.Copy(
                    workingPath,
                    destinationPath,
                    overwrite: false);

                if (!File.Exists(destinationPath))
                {
                    return OrganizationCopyResult.Failed(
                        "The organization copy was not found after the copy operation.");
                }

                FileInfo sourceInfo = new(workingPath);
                FileInfo destinationInfo = new(destinationPath);

                if (sourceInfo.Length != destinationInfo.Length)
                {
                    TryDelete(destinationPath);

                    return OrganizationCopyResult.Failed(
                        "The organization copy failed verification because its size does not match the working representation.");
                }

                if (!IsReadableEpub(destinationPath))
                {
                    TryDelete(destinationPath);

                    return OrganizationCopyResult.Failed(
                        "The organization copy failed EPUB verification.");
                }

                return OrganizationCopyResult.Success(destinationPath);
            }
            catch (Exception ex) when (
                ex is IOException ||
                ex is UnauthorizedAccessException ||
                ex is InvalidDataException)
            {
                TryDelete(destinationPath);

                return OrganizationCopyResult.Failed(ex.Message);
            }
        }

        private static bool IsReadableEpub(string path)
        {
            try
            {
                using ZipArchive archive =
                    ZipFile.OpenRead(path);

                return archive.Entries.Any();
            }
            catch (InvalidDataException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // The original working representation remains intact.
                // Failure to remove a bad destination is reported by the
                // failed operation without risking the working copy.
            }
        }
    }

    internal sealed class OrganizationCopyResult
    {
        private OrganizationCopyResult(
            bool succeeded,
            bool isPending,
            string? destinationPath,
            string? error,
            string? pendingReason)
        {
            Succeeded = succeeded;
            IsPending = isPending;
            DestinationPath = destinationPath;
            Error = error;
            PendingReason = pendingReason;
        }

        public bool Succeeded { get; }

        /// <summary>
        /// Indicates that the planned work is valid but cannot execute yet.
        /// Pending is not a failure and should not be treated as one by
        /// collection-level orchestration.
        /// </summary>
        public bool IsPending { get; }

        public string? DestinationPath { get; }

        public string? Error { get; }

        public string? PendingReason { get; }

        public static OrganizationCopyResult Success(
            string destinationPath) =>
            new(true, false, destinationPath, null, null);

        public static OrganizationCopyResult Pending(
            string reason) =>
            new(false, true, null, null, reason);

        public static OrganizationCopyResult Failed(
            string error) =>
            new(false, false, null, error, null);
    }
}
