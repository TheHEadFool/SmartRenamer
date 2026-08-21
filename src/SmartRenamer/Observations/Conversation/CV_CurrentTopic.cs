using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Scout.Observations.Conversation
{
    /// <summary>
    /// =========================================================================
    /// CV_CurrentTopic
    /// =========================================================================
    ///
    /// Motto
    /// -------------------------------------------------------------------------
    /// "Stay focused on the current goal."
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Represents the recommendation Scout is currently discussing
    /// with the user.
    ///
    /// Future Responsibilities
    /// -------------------------------------------------------------------------
    /// • Track the active recommendation.
    /// • Track the current question.
    /// • Track the user's response.
    /// • Know whether the recommendation has been accepted,
    ///   postponed, or declined.
    /// • Allow Scout to naturally change topics during the expedition.
    ///
    /// This class does NOT
    /// -------------------------------------------------------------------------
    /// • Decide what recommendation comes next.
    /// • Analyze files.
    /// • Translate ExpertFindings.
    /// • Render the user interface.
    ///
    /// Those responsibilities belong to the Recommendation Selector,
    /// Experts, Translators, Conversation Planner,
    /// and the User Interface.
    /// =========================================================================
    /// </summary>
    public sealed class CV_CurrentTopic : INotifyPropertyChanged
    {
        private CV_Recommendation? recommendation;

        /// <summary>
        /// The recommendation currently being discussed.
        /// </summary>
        public CV_Recommendation? Recommendation
        {
            get => recommendation;

            private set
            {
                if (ReferenceEquals(recommendation, value))
                    return;

                recommendation = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsActive));
            }
        }

        /// <summary>
        /// True once Scout has begun discussing this recommendation.
        /// </summary>
        public bool IsActive => Recommendation != null;

        /// <summary>
        /// Begin discussing a recommendation.
        /// </summary>
        public void Begin(CV_Recommendation recommendation)
        {
            Recommendation = recommendation ??
                throw new ArgumentNullException(nameof(recommendation));
        }

        /// <summary>
        /// End the current discussion.
        /// </summary>
        public void Clear()
        {
            Recommendation = null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}