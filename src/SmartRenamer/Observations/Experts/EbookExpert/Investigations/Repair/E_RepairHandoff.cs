using System;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// Represents the semantic result of the Ebook Expert repair stage
    /// for one EPUB.
    ///
    /// The organization stage receives this handoff rather than needing
    /// to understand the internal mechanics of the repair expedition.
    ///
    /// The original source path is preserved separately from the current
    /// working path so that organization never mistakes a temporary repair
    /// location for the user's source collection.
    /// </summary>
    internal sealed class E_RepairHandoff
    {
        /// <summary>
        /// The original source path of the EPUB.
        ///
        /// This identifies the user's protected original file.
        /// </summary>
        public string OriginalPath { get; }

        /// <summary>
        /// The path of the current working copy after the repair stage.
        ///
        /// This may be the same as the original path when no working copy
        /// was required, but organization must always treat OriginalPath
        /// as the protected source identity.
        /// </summary>
        public string WorkingPath { get; }

        /// <summary>
        /// The outcome of the repair stage for this EPUB.
        /// </summary>
        public E_RepairHandoffStatus Status { get; }

        /// <summary>
        /// A concise explanation of why the EPUB received this outcome.
        /// </summary>
        public string Reason { get; }

        public E_RepairHandoff(
            string originalPath,
            string workingPath,
            E_RepairHandoffStatus status,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(originalPath))
                throw new ArgumentException(
                    "The original path cannot be empty.",
                    nameof(originalPath));

            if (string.IsNullOrWhiteSpace(workingPath))
                throw new ArgumentException(
                    "The working path cannot be empty.",
                    nameof(workingPath));

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException(
                    "The reason cannot be empty.",
                    nameof(reason));

            OriginalPath = originalPath;
            WorkingPath = workingPath;
            Status = status;
            Reason = reason;
        }
    }

    /// <summary>
    /// Describes what happened to an EPUB during the repair stage.
    ///
    /// These values are deliberately semantic. The organization stage
    /// should not need to know which repair specialist, service, or action
    /// produced the result.
    /// </summary>
    internal enum E_RepairHandoffStatus
    {
        /// <summary>
        /// Repair processing completed successfully.
        /// </summary>
        RepairCompleted,

        /// <summary>
        /// Repair processing was intentionally deferred.
        /// </summary>
        RepairDeferred,

        /// <summary>
        /// Repair processing completed its available work but the EPUB
        /// remains unresolved.
        /// </summary>
        RepairUnresolved
    }
}