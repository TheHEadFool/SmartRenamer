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
        // Expedition state
        //---------------------------------------------------------

        private readonly Queue<FileContext> _pending = new();

        private readonly List<FileContext> _deferred = new();

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

        //---------------------------------------------------------
        // Deferred EPUBs
        //---------------------------------------------------------

        public IReadOnlyList<FileContext> DeferredFiles =>
            _deferred;

        //---------------------------------------------------------
        // State
        //---------------------------------------------------------

        public bool IsActive =>
            CurrentFile != null || _pending.Count > 0;

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

            CurrentFile = null;

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
            if (_pending.Count == 0)
            {
                CurrentFile = null;
                return null;
            }

            CurrentFile =
                _pending.Dequeue();

            return CurrentFile;
        }

        //---------------------------------------------------------
        // Defer
        //---------------------------------------------------------

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

            CurrentFile = null;
            SourceFolderPath = null;
        }
    }
}