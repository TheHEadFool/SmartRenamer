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
    /// • Track the state of individual repair opportunities.
    /// • Preserve the original source folder identity.
    /// • Preserve the current confidence threshold for this expedition.
    /// • Control progressive confidence-threshold lowering.
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

        public const double NormalConfidenceThreshold = 1.00;

        public const double MinimumConfidenceThreshold = 0.50;

        private const double ConfidenceThresholdStep = 0.10;

        public double CurrentConfidenceThreshold { get; private set; } =
            NormalConfidenceThreshold;

        //---------------------------------------------------------
        // Expedition state
        //---------------------------------------------------------

        private readonly Queue<FileContext> _pending = new();

        private readonly List<FileContext> _deferred = new();

        private readonly HashSet<string> _completed = new(
            StringComparer.OrdinalIgnoreCase);

        //---------------------------------------------------------
        // Repair opportunity state
        //---------------------------------------------------------

        //
        // Keyed by the original EPUB path.
        //
        // The original path is the stable identity of the EPUB even when
        // FileContext.CurrentFullPath later points to a repaired working copy.
        //
        //---------------------------------------------------------

        private readonly Dictionary<string, List<RepairOpportunityState>>
            _opportunityStates =
                new(StringComparer.OrdinalIgnoreCase);

        //---------------------------------------------------------
        // Source folder identity
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

        public IReadOnlyCollection<string> CompletedFiles =>
            _completed;

        //---------------------------------------------------------
        // Repair opportunity states
        //---------------------------------------------------------

        public IReadOnlyList<RepairOpportunityState> GetOpportunityStates(
            string originalPath)
        {
            if (string.IsNullOrWhiteSpace(originalPath))
                return Array.Empty<RepairOpportunityState>();

            if (!_opportunityStates.TryGetValue(
                    originalPath,
                    out List<RepairOpportunityState>? states))
            {
                return Array.Empty<RepairOpportunityState>();
            }

            return states;
        }

        public void SetOpportunityStates(
            string originalPath,
            IEnumerable<RepairOpportunityState> states)
        {
            if (string.IsNullOrWhiteSpace(originalPath))
                throw new ArgumentException(
                    "The original EPUB path cannot be empty.",
                    nameof(originalPath));

            if (states == null)
                throw new ArgumentNullException(nameof(states));

            _opportunityStates[originalPath] =
                new List<RepairOpportunityState>(states);
        }

        //---------------------------------------------------------
        // Confidence policy control
        //---------------------------------------------------------

        /// <summary>
        /// Lowers the confidence threshold for this expedition by one
        /// policy step.
        ///
        /// The threshold can never fall below the expedition minimum.
        ///
        /// Returns the resulting threshold.
        /// </summary>
        public double LowerConfidenceThreshold()
        {
            CurrentConfidenceThreshold =
                Math.Max(
                    MinimumConfidenceThreshold,
                    CurrentConfidenceThreshold -
                        ConfidenceThresholdStep);

            return CurrentConfidenceThreshold;
        }

        /// <summary>
        /// Restores the expedition to its normal confidence threshold.
        /// </summary>
        public void ResetConfidenceThreshold()
        {
            CurrentConfidenceThreshold =
                NormalConfidenceThreshold;
        }

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
            _completed.Clear();
            _opportunityStates.Clear();

            CurrentFile = null;

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
        // Complete / Defer
        //---------------------------------------------------------

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
            _completed.Clear();
            _opportunityStates.Clear();

            CurrentFile = null;
            SourceFolderPath = null;

            CurrentConfidenceThreshold =
                NormalConfidenceThreshold;
        }
    }
}