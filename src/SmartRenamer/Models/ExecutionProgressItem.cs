using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SmartRenamer.Models
{
    /// <summary>
    /// Live presentation model for one item participating in a collection
    /// operation. The item remains stable while its values change so the UI
    /// can update one row without rebuilding the entire collection.
    /// </summary>
    public sealed class ExecutionProgressItem : INotifyPropertyChanged
    {
        private string key = "";
        private string displayName = "";
        private string state = "Pending";
        private string status = "";
        private int completed;
        private int total;
        private IReadOnlyList<ExecutionProgressAction> actions =
            Array.Empty<ExecutionProgressAction>();
        private bool isPinned;

        public string Key
        {
            get => key;
            init => key = value ?? "";
        }

        public string DisplayName
        {
            get => displayName;
            internal set => Set(ref displayName, value ?? "");
        }

        public string State
        {
            get => state;
            internal set
            {
                if (Set(ref state, value ?? "Pending"))
                    OnPropertyChanged(nameof(DisplayPriority));
            }
        }

        public string Status
        {
            get => status;
            internal set => Set(ref status, value ?? "");
        }

        public int Completed
        {
            get => completed;
            internal set
            {
                if (Set(ref completed, value))
                    OnPropertyChanged(nameof(PercentComplete));
            }
        }

        public int Total
        {
            get => total;
            internal set
            {
                if (Set(ref total, value))
                    OnPropertyChanged(nameof(PercentComplete));
            }
        }

        public IReadOnlyList<ExecutionProgressAction> Actions
        {
            get => actions;
            internal set
            {
                actions = value ?? Array.Empty<ExecutionProgressAction>();
                OnPropertyChanged();
                OnPropertyChanged(nameof(NeedsUserAttention));
                OnPropertyChanged(nameof(DisplayPriority));
            }
        }

        public bool IsPinned
        {
            get => isPinned;
            internal set
            {
                if (Set(ref isPinned, value))
                    OnPropertyChanged(nameof(DisplayPriority));
            }
        }

        public bool NeedsUserAttention =>
            Actions.Count > 0;

        public bool IsCompleted =>
            string.Equals(State, "Organized", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(State, "Accepted", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(State, "Omitted", StringComparison.OrdinalIgnoreCase);

        public int DisplayPriority =>
            IsPinned ? 0 :
            NeedsUserAttention ? 1 :
            IsCompleted ? 4 :
            string.Equals(State, "Processing", StringComparison.OrdinalIgnoreCase) ? 2 :
            3;

        public int PercentComplete =>
            Total <= 0 ? 0 : Completed * 100 / Total;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void UpdateFrom(ExecutionProgressItem snapshot)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            DisplayName = snapshot.DisplayName;
            State = snapshot.State;
            Status = snapshot.Status;
            Completed = snapshot.Completed;
            Total = snapshot.Total;
            Actions = snapshot.Actions;
        }

        private bool Set<T>(
            ref T field,
            T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
