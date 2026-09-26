using System;
using System.Collections.Generic;
using SmartRenamer.Models;

namespace SmartRenamer.Observations.Experts.EbookExpert.Investigations.Repair
{
    /// <summary>
    /// =========================================================================
    /// E_RepairExpedition
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Maintains the state of a folder-level Ebook Expert repair expedition.
    ///
    /// An expedition is different from repairing one ebook.
    ///
    /// The Repair Service knows how to repair one ebook.
    /// The Expedition knows which ebooks still need attention and which ebooks
    /// must be deferred so the rest of the folder can continue.
    ///
    /// Responsibilities
    /// -------------------------------------------------------------------------
    /// • Hold the EPUBs participating in the expedition.
    /// • Track the current EPUB.
    /// • Track EPUBs still waiting to be processed.
    /// • Track EPUBs that require user input.
    /// • Preserve the original source folder identity.
    /// • Preserve the current confidence threshold for this expedition.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Perform repairs.
    /// • Perform ISBN research.
    /// • Decide which ISBN is correct.
    /// • Modify EPUB files.
    /// • Organize files.
    /// • Communicate with the Conversation Framework.
    ///
    /// Those responsibilities remain with the appropriate Ebook Expert
    /// services and the generic workflow/conversation layers.
    ///
    /// =========================================================================
    /// </summary>
    internal sealed class E_RepairExpedition
    {
        //---------------------------------------------------------
        // Confidence policy
        //---------------------------------------------------------
        //
        // The normal threshold is deliberately separate from the
        // minimum safety floor.
        //
        // The expedition will eventually be able to lower the
        // CurrentConfidenceThreshold progressively when unresolved
        // EPUBs remain.
        //
        // The 50% floor is not the normal operating threshold.
        //
        //---------------------------------------------------------

        public const double NormalConfidenceThreshold = 1.00;

        public const double MinimumConfidenceThreshold = 0.50;

        public double CurrentConfidenceThreshold { get; private set; } =
            NormalConfidenceThreshold;

        //---------------------------------------------------------
        // Expedition state
        //---------------------------------------------------------

        private readonly Queue<FileContext> _pending = new();

        private readonly List<FileContext> _deferred = new();

        // Each active EPUB is tracked independently by its stable original path.
        // CurrentFile remains as a compatibility cursor for the existing UI and
        // workflow while callers migrate to branch-aware operations.
        private readonly Dictionary<string, FileContext> _active =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> _completed = new(
            StringComparer.OrdinalIgnoreCase);

        //---------------------------------------------------------
        // Source folder identity
        //---------------------------------------------------------
        //
        // This identifies the user's original project folder.
        //
        // It is deliberately separate from FileContext.CurrentFullPath.
        //
        // A repaired EPUB may temporarily live in the Ebook Expert repair
        // workspace, but that must never cause the eventual organization
        // destination to be based on the temporary workspace.
        //
        //---------------------------------------------------------

        public string? SourceFolderPath { get; private set; }

        //---------------------------------------------------------
        // Current EPUB
        //---------------------------------------------------------

        public FileContext? CurrentFile { get; private set; }

        /// <summary>
        /// All EPUBs currently active in the expedition, keyed by OriginalFullPath.
        /// This is the branch-aware state; CurrentFile is retained as a compatibility
        /// cursor for existing single-book callers.
        /// </summary>
        public IReadOnlyCollection<FileContext> ActiveFiles => _active.Values;

        //---------------------------------------------------------
        // Deferred EPUBs
        //---------------------------------------------------------

        public IReadOnlyList<FileContext> DeferredFiles =>
            _deferred;

        public IReadOnlyCollection<string> CompletedFiles => _completed;

        //---------------------------------------------------------
        // State
        //---------------------------------------------------------

        public bool IsActive =>
            _active.Count > 0 || _pending.Count > 0;

        public bool IsComplete =>
            !IsActive;

        public bool HasDeferredFiles =>
            _deferred.Count > 0;

        public int PendingCount =>
            _pending.Count;

        //---------------------------------------------------------
        // Begin
        //---------------------------------------------------------

        /// <summary>
        /// Starts a new folder-level Ebook repair expedition.
        ///
        /// The supplied files become the expedition's pending queue.
        /// No repair is performed here.
        /// </summary>
        public void Begin(
            string sourceFolderPath,
            IEnumerable<FileContext> files)
        {
            if (string.IsNullOrWhiteSpace(sourceFolderPath))
                throw new ArgumentException(
                    "The source folder path cannot be empty.",
                    nameof(sourceFolderPath));

            if (files == null)
                throw new ArgumentNullException(nameof(files));

            _pending.Clear();
            _deferred.Clear();
            _active.Clear();
            _completed.Clear();

            CurrentFile = null;

            // Every new expedition begins at the normal confidence threshold.
            CurrentConfidenceThreshold =
                NormalConfidenceThreshold;

            SourceFolderPath =
                sourceFolderPath;

            foreach (FileContext file in files)
            {
                if (file == null)
                    continue;

                if (!string.Equals(
                        System.IO.Path.GetExtension(
                            file.OriginalFullPath),
                        ".epub",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                _pending.Enqueue(file);
            }

            MoveNext();
        }

        //---------------------------------------------------------
        // Branch activation / compatibility cursor
        //---------------------------------------------------------

        /// <summary>
        /// Activates the next pending EPUB without making it the only active EPUB.
        /// Returns the activated branch, or null when no EPUB remains pending.
        /// </summary>
        public FileContext? ActivateNext()
        {
            if (_pending.Count == 0)
                return null;

            FileContext file = _pending.Dequeue();

            if (string.IsNullOrWhiteSpace(file.OriginalFullPath))
                throw new InvalidOperationException(
                    "Cannot activate an EPUB because its original path is missing.");

            _active[file.OriginalFullPath] = file;
            CurrentFile = file;
            return file;
        }

        /// <summary>
        /// Returns the active branch for the specified original path.
        /// </summary>
        public FileContext? GetActive(string originalFullPath)
        {
            if (string.IsNullOrWhiteSpace(originalFullPath))
                return null;

            return _active.TryGetValue(originalFullPath, out FileContext? file)
                ? file
                : null;
        }

        //---------------------------------------------------------
        // Advance
        //---------------------------------------------------------

        /// <summary>
        /// Moves the expedition to the next EPUB waiting to be processed.
        ///
        /// Returns the new current EPUB, or null when the pending queue
        /// has been exhausted.
        /// </summary>
        public FileContext? MoveNext()
        {
            return ActivateNext();
        }

        //---------------------------------------------------------
        // Complete / Defer
        //---------------------------------------------------------

        /// <summary>
        /// Completes the active EPUB identified by its stable original path.
        ///
        /// This is the branch-aware counterpart to CompleteCurrent(). It does
        /// not depend on the compatibility CurrentFile cursor, which may point
        /// at a different active branch after concurrency is introduced.
        /// </summary>
        public bool Complete(string originalFullPath)
        {
            if (string.IsNullOrWhiteSpace(originalFullPath))
                return false;

            if (!_active.TryGetValue(
                    originalFullPath,
                    out FileContext? file))
                return false;

            _completed.Add(originalFullPath);
            _active.Remove(originalFullPath);

            if (ReferenceEquals(CurrentFile, file))
            {
                CurrentFile = null;
                MoveNext();
            }

            return true;
        }

        /// <summary>
        /// Marks the current EPUB as completed and advances to the next EPUB.
        /// </summary>
        public void CompleteCurrent()
        {
            if (CurrentFile == null)
                throw new InvalidOperationException(
                    "Cannot complete an EPUB because there is no current EPUB.");

            string originalPath = CurrentFile.OriginalFullPath;

            if (string.IsNullOrWhiteSpace(originalPath))
                throw new InvalidOperationException(
                    "Cannot complete the current EPUB because its original path is missing.");

            _completed.Add(originalPath);
            _active.Remove(originalPath);

            CurrentFile = null;

            MoveNext();
        }

        /// <summary>
        /// Removes the current EPUB from active processing and places it
        /// at the end of the expedition's deferred work.
        ///
        /// The expedition can therefore continue with the next EPUB instead
        /// of stopping the entire folder operation.
        /// </summary>
        public void DeferCurrent()
        {
            if (CurrentFile == null)
                return;

            _deferred.Add(CurrentFile);
            _active.Remove(CurrentFile.OriginalFullPath);

            CurrentFile = null;

            MoveNext();
        }

        //---------------------------------------------------------
        // Reset
        //---------------------------------------------------------

        /// <summary>
        /// Clears the expedition state.
        /// </summary>
        public void Reset()
        {
            _pending.Clear();
            _deferred.Clear();
            _active.Clear();
            _completed.Clear();

            CurrentFile = null;
            SourceFolderPath = null;

            CurrentConfidenceThreshold =
                NormalConfidenceThreshold;
        }
    }
}