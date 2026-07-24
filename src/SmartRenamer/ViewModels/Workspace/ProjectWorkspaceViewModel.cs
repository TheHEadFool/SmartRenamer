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

            Observations.Clear();

            SelectedObservation = null;

            int recommendationCount = 0;

            foreach (ProjectObservation observation in
                result.Project.Observations
                      .OrderByDescending(o => o.Priority)
                      .ThenBy(o => o.Title))
            {
                if (recommendationCount < 2)
                {
                    observation.IsRecommended = true;
                    observation.IsSelected = true;
                    recommendationCount++;
                }
                else
                {
                    observation.IsRecommended = false;
                    observation.IsSelected = false;
                }

                observation.ActionTitle = $"Organize {observation.Title}";
                observation.ActionDescription = observation.Description;

                Observations.Add(observation);
            }

            SelectedObservation = Observations.FirstOrDefault();

            //------------------------------------------
            // Scout Recommendations
            //------------------------------------------

           
            //------------------------------------------
            // Rename Preview
            //------------------------------------------

            RenamePreview.Clear();

            foreach (RenamePreview preview in result.Preview)
            {
                if (!preview.WillRename)
                    continue;

                RenamePreview.Add(preview);
            }

            //------------------------------------------
            // Summary
            //------------------------------------------

            if (RenamePreview.Count == 0)
            {
                Description =
                    "Scout analyzed this folder and didn't find any filenames that need changing.";
            }
            else if (RenamePreview.Count == 1)
            {
                Description =
                    "Scout analyzed this folder and found 1 filename that could be improved.";
            }
            else
            {
                Description =
                    $"Scout analyzed this folder and found {RenamePreview.Count} filenames that could be improved.";
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