using Scout.Observations.Conversation;
using SmartRenamer.Infrastructure;
using SmartRenamer.Models;
using SmartRenamer.Models.Recommendations;
using SmartRenamer.Models.Rename;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace SmartRenamer.ViewModels.Workspace
{
    public class ProjectWorkspaceViewModel : ObservableObject
    {
        public ProjectWorkspaceViewModel()
        {
            SelectObservationCommand = new RelayCommand(parameter =>
            {
                if (parameter is ProjectObservation observation)
                {
                    SelectObservation(observation);
                }
            });

            ReviewAllCommand = new RelayCommand(
                parameter => ReviewAll());
        }
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

        public ObservableCollection<ProjectObservation> Observations { get; }
    = new();

        public ObservableCollection<ProjectObservation> ReviewAllObservations { get; }
    = new();

        public ICommand ReviewAllCommand { get; }
        public ICommand SelectObservationCommand { get; }

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

        public void SelectObservation(ProjectObservation observation)
        {
            if (observation == null)
                return;

            System.Diagnostics.Debug.WriteLine(
                $"SelectObservation called: {observation.Title}");

            SelectedObservation = observation;
        }

        public void ReviewAll()
        {
            System.Diagnostics.Debug.WriteLine(
                "ReviewAll called.");

            ReviewAllObservations.Clear();

            foreach (ProjectObservation observation in Observations
                .Where(o => o.IsRecommended))
            {
                ReviewAllObservations.Add(observation);
            }

            System.Diagnostics.Debug.WriteLine(
                $"ReviewAll gathered {ReviewAllObservations.Count} observations.");
        }

        //---------------------------------------------------------
        // Conversation
        //---------------------------------------------------------

        public CV_CurrentTopic CurrentTopic { get; }
            = new();

        //---------------------------------------------------------
        // Legacy Recommendation Panel
        // TODO (Generation 2)
        //
        // Replace this collection with the Conversation Framework.
        // The UI should ultimately bind to CurrentTopic instead of
        // a collection of Recommendation objects.
        //---------------------------------------------------------

        public ObservableCollection<Recommendation> Recommendations { get; }
            = new();

        public ObservableCollection<RenamePreview> RenamePreview { get; }
            = new();

        public int RenameCount => RenamePreview.Count;

        public bool HasRenamePreview => RenamePreview.Count > 0;

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
            // Recommendation status belongs to the observation itself.
            //------------------------------------------

            Observations.Clear();

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
            // Scout Recommendations
            //------------------------------------------


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
                    $"Scout analyzed this folder and prepared {RenamePreview.Count} organizational changes.";
            }

            OnPropertyChanged(nameof(RenameCount));
            OnPropertyChanged(nameof(HasRenamePreview));

            //------------------------------------------
            // Recommendations
            //------------------------------------------

            Recommendations.Clear();

            foreach (Recommendation recommendation in recommendations)
            {
                Recommendations.Add(recommendation);
            }
        }
    }
}