using Scout.Observations.Conversation;
using SmartRenamer.Infrastructure;
using SmartRenamer.Models;
using SmartRenamer.Models.Recommendations;
using SmartRenamer.Models.Rename;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace SmartRenamer.ViewModels.Workspace
{
    /// <summary>
    /// =========================================================================
    /// ProjectWorkspaceViewModel
    /// =========================================================================
    ///
    /// Purpose
    /// -------------------------------------------------------------------------
    /// Coordinates the Workspace presentation of Scout's project observations,
    /// recommendations, Review All report, and conversation state.
    ///
    /// =========================================================================
    /// MIGRATION STATUS
    /// =========================================================================
    ///
    /// This ViewModel currently supports BOTH systems:
    ///
    /// LEGACY:
    ///
    ///     ProjectObservation
    ///          ↓
    ///     ReviewAllObservations
    ///          ↓
    ///     ReviewAllRequested
    ///
    /// NEW:
    ///
    ///     CV_Recommendation
    ///          ↓
    ///     CV_ReviewAllItem
    ///          ↓
    ///     Conversation Framework
    ///
    /// The legacy path remains intentionally available until the new
    /// Conversation Framework is fully connected to the Workspace UI.
    ///
    /// DO NOT remove the legacy properties or event until the new Review All
    /// report has been proven in the running application.
    ///
    /// =========================================================================
    /// REVIEW ALL DESIGN
    /// =========================================================================
    ///
    /// Review All is NOT a sequential conversation.
    ///
    /// It is intended to produce one complete report containing all findings.
    ///
    /// Scout remains available beside the report and can discuss an individual
    /// finding when the user asks.
    ///
    /// The eventual flow is:
    ///
    ///     Review All
    ///          ↓
    ///     Complete Report
    ///          ↓
    ///     Finding link
    ///          ↓
    ///     Matching Workspace observation
    ///          ↓
    ///     Scout conversation
    ///
    /// =========================================================================
    /// </summary>
    public class ProjectWorkspaceViewModel : ObservableObject
    {
        //---------------------------------------------------------
        // Constructor
        //---------------------------------------------------------

        public ProjectWorkspaceViewModel()
        {
            SelectObservationCommand = new RelayCommand(parameter =>
            {
                if (parameter is ProjectObservation observation)
                {
                    SelectObservation(observation);
                }
            });

            SelectReviewAllItemCommand = new RelayCommand(parameter =>
            {
                if (parameter is CV_ReviewAllItem item)
                {
                    SelectReviewAllItem(item);
                }
            });

            ReviewAllCommand = new RelayCommand(
                parameter => ReviewAll());
        }

        //---------------------------------------------------------
        // Project Presentation
        //---------------------------------------------------------

        private string title = "The Plan";

        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        private string description =
            "Scout will build workflows and previews here.";

        public string Description
        {
            get => description;
            set => SetProperty(ref description, value);
        }

        private string nextStep = "";

        public string NextStep
        {
            get => nextStep;
            set => SetProperty(ref nextStep, value);
        }

        //---------------------------------------------------------
        // Observations
        //---------------------------------------------------------

        public ObservableCollection<ProjectObservation> Observations { get; }
            = new();

        //---------------------------------------------------------
        // LEGACY REVIEW ALL OBSERVATIONS
        //---------------------------------------------------------
        //
        // Retained during migration.
        //
        // This is still used by the existing Workspace UI.
        //
        //---------------------------------------------------------

        public ObservableCollection<ProjectObservation> ReviewAllObservations { get; }
            = new();

        //---------------------------------------------------------
        // NEW REVIEW ALL REPORT
        //---------------------------------------------------------
        //
        // This is the new Conversation Framework representation.
        //
        // It contains every CV_ReviewAllItem produced by the
        // ConversationEngine.
        //
        //---------------------------------------------------------

        public ObservableCollection<CV_ReviewAllItem> ReviewAllItems { get; }
            = new();

        //---------------------------------------------------------
        // Review All State
        //---------------------------------------------------------

        private bool isReviewAllActive;

        public bool IsReviewAllActive
        {
            get => isReviewAllActive;
            private set => SetProperty(ref isReviewAllActive, value);
        }

        //---------------------------------------------------------
        // Commands
        //---------------------------------------------------------

        public ICommand ReviewAllCommand { get; }

        public ICommand SelectObservationCommand { get; }

        public ICommand SelectReviewAllItemCommand { get; }

        //---------------------------------------------------------
        // Legacy Review All Event
        //---------------------------------------------------------
        //
        // Retained until the new Review All report is connected directly
        // to the UI.
        //
        //---------------------------------------------------------

        public event EventHandler? ReviewAllRequested;
        public event EventHandler<CV_ConversationMessage>? ConversationMessageGenerated;

        //---------------------------------------------------------
        // Conversation
        //---------------------------------------------------------

        /// <summary>
        /// Conversation Engine used by the new Review All system.
        ///
        /// The engine owns the complete set of recommendations and builds
        /// the Review All collection.
        /// </summary>
        public CV_ConversationEngine ConversationEngine { get; }
            = new();

        /// <summary>
        /// Current focused conversation topic.
        /// </summary>
        public CV_CurrentTopic CurrentTopic =>
            ConversationEngine.CurrentTopic;

        //---------------------------------------------------------
        // Selection
        //---------------------------------------------------------

        private ProjectObservation? selectedObservation;

        public ProjectObservation? SelectedObservation
        {
            get => selectedObservation;

            set
            {
                if (SetProperty(ref selectedObservation, value))
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Selected Observation: {value?.Title}");
                }
            }
        }

        //---------------------------------------------------------
        // Currently selected Expert recommendation
        //---------------------------------------------------------

        private CV_Recommendation? selectedRecommendation;

        public CV_Recommendation? SelectedRecommendation
        {
            get => selectedRecommendation;
            private set => SetProperty(
                ref selectedRecommendation,
                value);
        }

        /// <summary>
        /// Selects an observation in the Workspace.
        ///
        /// This remains UI state only. The ViewModel does not decide
        /// which finding is important.
        /// </summary>
        public void SelectObservation(
            ProjectObservation observation)
        {
            if (observation == null)
                return;

            IsReviewAllActive = false;

            System.Diagnostics.Debug.WriteLine(
                $"SelectObservation called: {observation.Title}");

            //---------------------------------------------------------
            // Clear previous selection.
            //---------------------------------------------------------

            foreach (ProjectObservation item in Observations)
            {
                item.IsSelected =
                    ReferenceEquals(item, observation);
            }

            SelectedObservation = observation;
            //---------------------------------------------------------
            // Keep Scout's conversation synchronized with the
            // observation selected in the Workspace.
            //---------------------------------------------------------

            foreach (CV_Recommendation recommendation in ConversationEngine.Recommendations)
            {
                if (recommendation.Id == observation.Id)
                {
                    SelectedRecommendation = recommendation;

                    CV_ConversationMessage? message =
                        ConversationEngine.DiscussRecommendation(recommendation);

                    if (message != null)
                    {
                        ConversationMessageGenerated?.Invoke(
                            this,
                            message);
                    }

                    break;
                }
            }
        }

        //---------------------------------------------------------
        // Review All
        //---------------------------------------------------------

        /// <summary>
        /// Begins the new Review All report.
        ///
        /// The Conversation Engine creates a complete collection from
        /// every recommendation it owns.
        ///
        /// The legacy ReviewAllObservations collection is also populated
        /// temporarily so that the existing UI continues to function while
        /// migration is underway.
        /// </summary>
        public void ReviewAll()
        {
            System.Diagnostics.Debug.WriteLine(
                "ReviewAll called.");

            IsReviewAllActive = true;

            //---------------------------------------------------------
            // NEW CONVERSATION FRAMEWORK
            //---------------------------------------------------------
            //
            // Build the complete Review All collection.
            //
            //---------------------------------------------------------

            ConversationEngine.ReviewAll();

            ReviewAllItems.Clear();

            foreach (CV_ReviewAllItem item in
                ConversationEngine.ReviewAllItems)
            {
                ProjectObservation? matchingObservation =
                    Observations.FirstOrDefault(
                        observation =>
                            observation.Id == item.Id);

                ReviewAllItems.Add(
                    new CV_ReviewAllItem(
                        item.Recommendation,
                        matchingObservation));
            }

            System.Diagnostics.Debug.WriteLine(
                $"Conversation Review All gathered " +
                $"{ReviewAllItems.Count} recommendations.");

            //---------------------------------------------------------
            // LEGACY REVIEW ALL PATH
            //---------------------------------------------------------
            //
            // Keep this temporarily so the existing Workspace continues
            // to display its current Review All observations.
            //
            //---------------------------------------------------------

            ReviewAllObservations.Clear();

            foreach (ProjectObservation observation in
                Observations.Where(o => o.IsRecommended))
            {
                ReviewAllObservations.Add(observation);
            }

            System.Diagnostics.Debug.WriteLine(
                $"Legacy Review All gathered " +
                $"{ReviewAllObservations.Count} observations.");

            //---------------------------------------------------------
            // Existing UI selection behavior.
            //
            // This remains temporarily during migration.
            //---------------------------------------------------------

            SelectedObservation =
                ReviewAllObservations.FirstOrDefault();

            if (SelectedObservation != null)
            {
                SelectedObservation.IsSelected = true;
            }

            //---------------------------------------------------------
            // Existing event.
            //
            // Do NOT remove yet. The current UI may still depend on it.
            //---------------------------------------------------------

            ReviewAllRequested?.Invoke(
                this,
                EventArgs.Empty);
        }

        //---------------------------------------------------------
        // Select Review All Item
        //---------------------------------------------------------

        /// <summary>
        /// Selects the Workspace observation represented by a Review All item.
        ///
        /// This is the beginning of the hyperlink bridge.
        ///
        /// Review All does not create another finding. It identifies the
        /// existing Workspace observation and selects it.
        /// </summary>
        public void SelectReviewAllItem(
            CV_ReviewAllItem item)
        {
            if (item == null)
                return;

            //---------------------------------------------------------
            // If the item already contains its corresponding observation,
            // use it directly.
            //---------------------------------------------------------

            ProjectObservation? observation =
                item.Observation;

            //---------------------------------------------------------
            // During migration, fall back to the shared finding identity.
            //---------------------------------------------------------

            if (observation == null)
            {
                observation =
                    Observations.FirstOrDefault(
                        o => o.Id == item.Id);
            }

            if (observation == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"No Workspace observation found for Review All item " +
                    $"{item.Id}");

                return;
            }

            //---------------------------------------------------------
            // Select the same observation represented by the left-side
            // Workspace button.
            //---------------------------------------------------------

            SelectObservation(observation);

            //---------------------------------------------------------
            // Move Scout's conversation to this recommendation.
            //---------------------------------------------------------

            CurrentTopic.Begin(item.Recommendation);

            System.Diagnostics.Debug.WriteLine(
                $"Conversation moved to Review All recommendation: " +
                $"{item.Recommendation.Title}");
        }

        //---------------------------------------------------------
        // Legacy Recommendation Panel
        // TODO (Generation 2)
        //
        // Replace this collection with the Conversation Framework.
        // The UI should ultimately bind to CurrentTopic and ReviewAllItems
        // instead of the legacy Recommendation collection.
        //
        //---------------------------------------------------------

        public ObservableCollection<Recommendation> Recommendations { get; }
            = new();

        //---------------------------------------------------------
        // Rename Preview
        //---------------------------------------------------------

        public ObservableCollection<RenamePreview> RenamePreview { get; }
            = new();

        public int RenameCount =>
            RenamePreview.Count;

        public bool HasRenamePreview =>
            RenamePreview.Count > 0;

        //---------------------------------------------------------
        // Load
        //---------------------------------------------------------

        public void Load(
            WorkflowResult result,
            IEnumerable<Recommendation> recommendations)
        {
            if (result == null)
                return;

            //------------------------------------------
            // Project Summary
            //------------------------------------------


            //------------------------------------------
            // Observations
            //------------------------------------------
            //
            // BUTTON PRESENTATION PATH
            //
            // The ObservationMapper has already converted the authoritative
            // ExpertFindings into ProjectObservations.
            //
            // The ViewModel does NOT decide which findings are important.
            // It simply presents the observations supplied by the workflow.
            //
            // Every distinct observation remains available as a button.
            //
            // Selection is UI state only.
            //------------------------------------------

            IsReviewAllActive = false;

            Observations.Clear();

            ReviewAllObservations.Clear();

            ReviewAllItems.Clear();

            SelectedObservation = null;

            foreach (ProjectObservation observation in
                result.Project.Observations
                    .OrderByDescending(o => o.Priority)
                    .ThenBy(o => o.Title))
            {
                //------------------------------------------
                // Preserve recommendation state supplied
                // by the ObservationMapper.
                //------------------------------------------

                observation.IsSelected = false;

                //------------------------------------------
                // Add the observation to the button path.
                //------------------------------------------

                Observations.Add(observation);
            }

            //------------------------------------------
            // Select the first observation initially.
            //
            // This is UI state, not recommendation logic.
            //------------------------------------------

            SelectedObservation =
                Observations.FirstOrDefault();

            if (SelectedObservation != null)
            {
                SelectedObservation.IsSelected = true;
            }

            //------------------------------------------
            // Rename Preview
            //------------------------------------------

            RenamePreview.Clear();

            foreach (RenamePreview preview in result.Preview)
            {
                if (!preview.HasChanges)
                    continue;

                RenamePreview.Add(preview);
            }

            //------------------------------------------
            // Summary
            //------------------------------------------

            if (RenamePreview.Count == 0)
            {
                Description =
                    "Scout analyzed this folder and didn't find any organizational changes to recommend.";
            }
            else if (RenamePreview.Count == 1)
            {
                Description =
                    "Scout analyzed this folder and prepared 1 organizational change.";
            }
            else
            {
                Description =
                    $"Scout analyzed this folder and prepared " +
                    $"{RenamePreview.Count} organizational changes.";
            }

            OnPropertyChanged(nameof(RenameCount));
            OnPropertyChanged(nameof(HasRenamePreview));

            //------------------------------------------
            // Legacy Recommendations
            //------------------------------------------
            //
            // Retained during migration.
            //
            //------------------------------------------

            Recommendations.Clear();

            foreach (Recommendation recommendation in recommendations)
            {
                Recommendations.Add(recommendation);
            }

            //---------------------------------------------------------
            // Conversation Framework
            //---------------------------------------------------------
            //
            // The WorkflowResult already contains the authoritative
            // recommendations produced by the Observation Framework.
            //
            // Do not rebuild these from the legacy Recommendation
            // objects. The Conversation Engine should receive the
            // Expert-generated recommendations directly.
            //
            // This keeps Review All and the Conversation Framework
            // operating on the same recommendation set.
            //---------------------------------------------------------

            ConversationEngine.LoadRecommendations(
    result.ObservationRecommendations);

        }
    }
}