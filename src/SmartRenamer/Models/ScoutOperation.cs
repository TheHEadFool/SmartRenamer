using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Scout.Observations.Conversation;

namespace SmartRenamer.Models
{
    /// <summary>
    /// Represents a long-running operation that Scout is performing.
    /// </summary>
    public class ScoutOperation : INotifyPropertyChanged
    {
        private string title = "";
        private string currentTask = "";
        private string status = "";
        private string currentFile = "";

        private int completedSteps;
        private int totalSteps;

        // The collection instance is intentionally stable while an expedition
        // is running. WPF binds the Collection Progress ListBox directly to
        // this collection, so replacing the list on every progress tick can
        // force container teardown/recreation while WPF is already processing
        // visual-tree changes.
        private readonly ObservableCollection<ExecutionProgressItem> items =
            new();

        private string stage = "";
        private int stageCompleted;
        private int stageTotal;

        private int collectionTotal;
        private int collectionCompleted;
        private int collectionProcessing;
        private int collectionWaiting;
        private int collectionPending;

        private TimeSpan elapsedTime = TimeSpan.Zero;
        private TimeSpan? estimatedRemaining = null;

        private ScoutOperationState state = ScoutOperationState.Idle;

        #region General Information

        public string Title
        {
            get => title;
            set
            {
                if (title == value)
                    return;

                title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        public string CurrentTask
        {
            get => currentTask;
            set
            {
                if (currentTask == value)
                    return;

                currentTask = value;
                OnPropertyChanged(nameof(CurrentTask));
            }
        }

        public string Status
        {
            get => status;
            set
            {
                if (status == value)
                    return;

                status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public string CurrentFile
        {
            get => currentFile;
            set
            {
                if (currentFile == value)
                    return;

                currentFile = value;
                OnPropertyChanged(nameof(CurrentFile));
            }
        }

        #endregion

        #region Progress

        public int CompletedSteps
        {
            get => completedSteps;
            set
            {
                if (completedSteps == value)
                    return;

                completedSteps = value;

                OnPropertyChanged(nameof(CompletedSteps));
                OnPropertyChanged(nameof(PercentComplete));
            }
        }

        public int TotalSteps
        {
            get => totalSteps;
            set
            {
                if (totalSteps == value)
                    return;

                totalSteps = value;

                OnPropertyChanged(nameof(TotalSteps));
                OnPropertyChanged(nameof(PercentComplete));
            }
        }

        /// <summary>
        /// Collection completion is the primary operation percentage whenever
        /// a collection has been established. Stage progress remains available
        /// through StageCompleted/StageTotal but must not masquerade as
        /// collection completion.
        /// </summary>
        public int PercentComplete =>
            CollectionTotal > 0
                ? CollectionCompleted * 100 / CollectionTotal
                : TotalSteps == 0
                    ? 0
                    : CompletedSteps * 100 / TotalSteps;

        public string Stage
        {
            get => stage;
            set
            {
                if (stage == value)
                    return;

                stage = value;
                OnPropertyChanged(nameof(Stage));
            }
        }

        public int StageCompleted
        {
            get => stageCompleted;
            set
            {
                if (stageCompleted == value)
                    return;

                stageCompleted = value;
                OnPropertyChanged(nameof(StageCompleted));
            }
        }

        public int StageTotal
        {
            get => stageTotal;
            set
            {
                if (stageTotal == value)
                    return;

                stageTotal = value;
                OnPropertyChanged(nameof(StageTotal));
            }
        }

        public int CollectionTotal
        {
            get => collectionTotal;
            set
            {
                if (collectionTotal == value)
                    return;

                collectionTotal = value;
                OnPropertyChanged(nameof(CollectionTotal));
                OnPropertyChanged(nameof(PercentComplete));
            }
        }

        public int CollectionCompleted
        {
            get => collectionCompleted;
            set
            {
                if (collectionCompleted == value)
                    return;

                collectionCompleted = value;
                OnPropertyChanged(nameof(CollectionCompleted));
                OnPropertyChanged(nameof(PercentComplete));
            }
        }

        public int CollectionProcessing
        {
            get => collectionProcessing;
            set
            {
                if (collectionProcessing == value)
                    return;

                collectionProcessing = value;
                OnPropertyChanged(nameof(CollectionProcessing));
            }
        }

        public int CollectionWaiting
        {
            get => collectionWaiting;
            set
            {
                if (collectionWaiting == value)
                    return;

                collectionWaiting = value;
                OnPropertyChanged(nameof(CollectionWaiting));
            }
        }

        public int CollectionPending
        {
            get => collectionPending;
            set
            {
                if (collectionPending == value)
                    return;

                collectionPending = value;
                OnPropertyChanged(nameof(CollectionPending));
            }
        }

        public TimeSpan ElapsedTime
        {
            get => elapsedTime;
            set
            {
                if (elapsedTime == value)
                    return;

                elapsedTime = value;
                OnPropertyChanged(nameof(ElapsedTime));
            }
        }

        public TimeSpan? EstimatedRemaining
        {
            get => estimatedRemaining;
            set
            {
                if (estimatedRemaining == value)
                    return;

                estimatedRemaining = value;
                OnPropertyChanged(nameof(EstimatedRemaining));
            }
        }

        public IReadOnlyList<ExecutionProgressItem> Items =>
            items;

        // The Live Report deliberately exposes only three user-facing states.
        // Internal/domain states remain in ExecutionProgressItem.State.
        public IEnumerable<ExecutionProgressItem> NeedsItems =>
            items.Where(item => item.NeedsUserAttention);

        public IEnumerable<ExecutionProgressItem> WorkingItems =>
            items.Where(item => !item.NeedsUserAttention && !item.IsCompleted);

        public IEnumerable<ExecutionProgressItem> CompleteItems =>
            items.Where(item => item.IsCompleted);

        public bool HasNeedsItems =>
            items.Any(item => item.NeedsUserAttention);

        public bool HasWorkingItems =>
            items.Any(item => !item.NeedsUserAttention && !item.IsCompleted);

        public bool HasCompleteItems =>
            items.Any(item => item.IsCompleted);

        /// <summary>
        /// Applies the terminal result of a background research operation to
        /// the live report immediately. Background research can finish after
        /// the synchronous investigation has returned, so waiting for another
        /// full observation pass would leave a completed research item looking
        /// like WORKING indefinitely.
        /// </summary>
        public void ApplyBackgroundActionResult(
            string contextId,
            CV_ActionResult result)
        {
            if (string.IsNullOrWhiteSpace(contextId) || result == null)
                return;

            // A result that changed the protected working copy will be followed
            // by the normal re-observation path. Do not briefly overwrite that
            // authoritative snapshot with the older background result.
            if (result.RequiresReobservation)
                return;

            ExecutionProgressItem? item =
                items.FirstOrDefault(candidate =>
                    string.Equals(
                        candidate.Key,
                        contextId,
                        StringComparison.OrdinalIgnoreCase));

            if (item == null)
                return;

            List<ExecutionProgressAction> actions =
                result.Options
                    .Where(option => option != null)
                    .Select(option =>
                        new ExecutionProgressAction
                        {
                            Id = option.Id,
                            Label = option.Label,
                            ActionId = option.ActionId,
                            ContextId = option.ContextId
                        })
                    .ToList();

            if (actions.Count > 0)
            {
                item.State = "Unorganized";
            }
            else if (result.Success &&
                     string.Equals(
                         item.State,
                         "Researching",
                         StringComparison.OrdinalIgnoreCase))
            {
                // A background research operation that finishes without a
                // physical repair is no longer active work. Do not leave the
                // row in WORKING indefinitely. The next stage/organization
                // decision can consume the ready item.
                item.State = "Ready";
                item.Status = "Research complete; ready for the next step.";
            }
            else if (!result.Success)
            {
                // A provider/worker failure must not become a dead WORKING row
                // or fail the entire expedition. Give the user one safe way to
                // continue when the domain result did not supply a more specific
                // action.
                item.State = "Unorganized";
                actions.Add(
                    new ExecutionProgressAction
                    {
                        Id = $"AcceptAsIs:{contextId}",
                        Label = "Accept as-is",
                        ActionId = "AcceptAsIs",
                        ContextId = contextId
                    });
                item.Status = string.IsNullOrWhiteSpace(result.Message)
                    ? "Background research could not complete. Choose how to continue."
                    : result.Message + " Choose how to continue.";
            }

            if (!string.IsNullOrWhiteSpace(result.Message) &&
                result.Success &&
                actions.Count > 0)
            {
                item.Status = result.Message;
            }

            item.Completed = item.IsCompleted ? item.Total : 0;
            item.Total = Math.Max(1, item.Total);
            item.Actions = actions;

            RecalculateCollectionCountsFromItems();

            OnPropertyChanged(nameof(NeedsItems));
            OnPropertyChanged(nameof(WorkingItems));
            OnPropertyChanged(nameof(CompleteItems));
            OnPropertyChanged(nameof(HasNeedsItems));
            OnPropertyChanged(nameof(HasWorkingItems));
            OnPropertyChanged(nameof(HasCompleteItems));
        }

        private void RecalculateCollectionCountsFromItems()
        {
            if (items.Count == 0)
                return;

            int completed = items.Count(item => item.IsCompleted);
            int waiting = items.Count(item => item.NeedsUserAttention);
            int processing = items.Count(item =>
                string.Equals(
                    item.State,
                    "Researching",
                    StringComparison.OrdinalIgnoreCase));

            CollectionTotal = items.Count;
            CollectionCompleted = completed;
            CollectionWaiting = waiting;
            CollectionProcessing = processing;
            CollectionPending =
                Math.Max(
                    0,
                    items.Count - completed - waiting - processing);
        }

        /// <summary>
        /// Applies a progress snapshot without replacing the collection bound
        /// to the WPF Collection Progress ListBox. Existing rows are updated
        /// in place by stable Key, which allows WPF to keep its realized
        /// containers instead of tearing down and recreating the entire list
        /// on every progress report.
        ///
        /// The incoming snapshot may be sorted for calculation purposes, but
        /// this method deliberately does not reorder the live collection.
        /// Attention ordering is therefore not changed as a side effect of a
        /// progress tick. A later, explicitly scheduled presentation update
        /// can address ordering without coupling it to domain progress.
        /// </summary>
        public void ApplyItems(
            IReadOnlyList<ExecutionProgressItem>? snapshots)
        {
            snapshots ??= Array.Empty<ExecutionProgressItem>();

            bool wasEmpty =
                items.Count == 0;

            var snapshotsByKey =
                new Dictionary<string, ExecutionProgressItem>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (ExecutionProgressItem snapshot in snapshots)
            {
                if (snapshot == null ||
                    string.IsNullOrWhiteSpace(snapshot.Key))
                {
                    continue;
                }

                snapshotsByKey[snapshot.Key] = snapshot;
            }

            // Update rows that already exist. This is the normal path during
            // investigation and re-observation and produces only property
            // changes on the affected row.
            foreach (ExecutionProgressItem item in items)
            {
                if (snapshotsByKey.TryGetValue(item.Key, out ExecutionProgressItem? snapshot))
                {
                    item.UpdateFrom(snapshot);
                }
            }

            // A collection's membership should normally remain stable for an
            // expedition. These structural changes are retained for discovery
            // or reset scenarios, but they are deliberately not accompanied by
            // an ItemsSource replacement.
            for (int index = items.Count - 1; index >= 0; index--)
            {
                if (!snapshotsByKey.ContainsKey(items[index].Key))
                {
                    items.RemoveAt(index);
                }
            }

            var existingKeys =
                new HashSet<string>(
                    items.Select(item => item.Key),
                    StringComparer.OrdinalIgnoreCase);

            foreach (ExecutionProgressItem snapshot in snapshots)
            {
                if (snapshot == null ||
                    string.IsNullOrWhiteSpace(snapshot.Key) ||
                    existingKeys.Contains(snapshot.Key))
                {
                    continue;
                }

                items.Add(snapshot);
                existingKeys.Add(snapshot.Key);
            }

            // Row state may have changed without collection membership changing.
            // Notify the filtered Live Report views before/after repositioning.
            OnPropertyChanged(nameof(NeedsItems));
            OnPropertyChanged(nameof(WorkingItems));
            OnPropertyChanged(nameof(CompleteItems));
            OnPropertyChanged(nameof(HasNeedsItems));
            OnPropertyChanged(nameof(HasWorkingItems));
            OnPropertyChanged(nameof(HasCompleteItems));

            // Do not reorder the live ObservableCollection during progress ticks.
            // WPF can be in the middle of realizing/virtualizing containers when
            // a progress callback arrives; moving items here previously caused
            // VisualTreeChanged failures. The Live Report already separates
            // NEEDS, WORKING, and COMPLETE through filtered views, so stable
            // membership is more important than sorting the backing collection.

            OnPropertyChanged(nameof(HasCollectionItems));
            OnPropertyChanged(nameof(NeedsItems));
            OnPropertyChanged(nameof(WorkingItems));
            OnPropertyChanged(nameof(CompleteItems));
            OnPropertyChanged(nameof(HasNeedsItems));
            OnPropertyChanged(nameof(HasWorkingItems));
            OnPropertyChanged(nameof(HasCompleteItems));
        }

        private static int CompareDisplayItems(
            ExecutionProgressItem left,
            ExecutionProgressItem right)
        {
            int priority = left.DisplayPriority.CompareTo(right.DisplayPriority);
            if (priority != 0)
                return priority;

            int name = StringComparer.OrdinalIgnoreCase.Compare(
                left.DisplayName,
                right.DisplayName);

            if (name != 0)
                return name;

            return StringComparer.OrdinalIgnoreCase.Compare(
                left.Key,
                right.Key);
        }

        /// <summary>
        /// Marks every item in the collection as terminally organized.
        /// This is used only after the domain Expert reports that organization
        /// completed for the entire collection. It reconciles the generic Live
        /// Report with the domain terminal result without making the generic
        /// Scout layer understand ebook metadata.
        /// </summary>
        public void MarkAllItemsCompleted()
        {
            foreach (ExecutionProgressItem item in items)
            {
                if (item.NeedsUserAttention)
                    continue;

                item.State = "Organized";
                item.Status = "Complete";
                item.Completed = Math.Max(item.Completed, item.Total);
            }

            OnPropertyChanged(nameof(NeedsItems));
            OnPropertyChanged(nameof(WorkingItems));
            OnPropertyChanged(nameof(CompleteItems));
            OnPropertyChanged(nameof(HasNeedsItems));
            OnPropertyChanged(nameof(HasWorkingItems));
            OnPropertyChanged(nameof(HasCompleteItems));
            OnPropertyChanged(nameof(PercentComplete));
        }

        public bool HasCollectionItems =>
            items.Count > 0;

        #endregion

        #region State

        public ScoutOperationState State
        {
            get => state;
            set
            {
                if (state == value)
                    return;

                state = value;

                OnPropertyChanged(nameof(State));
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(IsFinished));
                OnPropertyChanged(nameof(CanCancel));
                OnPropertyChanged(nameof(CanPause));
            }
        }

        public bool IsRunning =>
            State == ScoutOperationState.Running ||
            State == ScoutOperationState.WaitingForUser;

        public bool IsFinished =>
            State == ScoutOperationState.Completed ||
            State == ScoutOperationState.Cancelled ||
            State == ScoutOperationState.Failed;

        public bool CanPause =>
            State == ScoutOperationState.Running;

        public bool CanCancel =>
            State == ScoutOperationState.Running ||
            State == ScoutOperationState.Paused ||
            State == ScoutOperationState.WaitingForUser;

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}