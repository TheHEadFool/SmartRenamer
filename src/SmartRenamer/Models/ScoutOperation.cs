using System;
using System.ComponentModel;

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

        public int PercentComplete =>
            TotalSteps == 0
                ? 0
                : CompletedSteps * 100 / TotalSteps;

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
            State == ScoutOperationState.Running;

        public bool IsFinished =>
            State == ScoutOperationState.Completed ||
            State == ScoutOperationState.Cancelled ||
            State == ScoutOperationState.Failed;

        public bool CanPause =>
            State == ScoutOperationState.Running;

        public bool CanCancel =>
            State == ScoutOperationState.Running ||
            State == ScoutOperationState.Paused;

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