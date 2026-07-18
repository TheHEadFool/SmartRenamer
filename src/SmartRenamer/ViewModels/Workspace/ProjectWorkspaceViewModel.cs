using System.Collections.ObjectModel;
using SmartRenamer.Infrastructure;
using SmartRenamer.Models;
using SmartRenamer.Models.Recommendations;
using SmartRenamer.Models.Rename;

namespace SmartRenamer.ViewModels.Workspace
{
    public class ProjectWorkspaceViewModel : ObservableObject
    {
        private string title = "Project Workspace";

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

        public ObservableCollection<string> Observations { get; }
            = new();

        public ObservableCollection<Recommendation> Recommendations { get; }
            = new();

        public ObservableCollection<RenamePreview> RenamePreview { get; }
            = new();

        public int RenameCount => RenamePreview.Count;

        public bool HasRenamePreview => RenamePreview.Count > 0;

        public void Load(WorkflowResult result)
        {
            if (result == null)
                return;

            //------------------------------------------
            // Project Summary
            //------------------------------------------

            Title = result.Project.ProjectType;

            //------------------------------------------
            // Observations
            //------------------------------------------

            Observations.Clear();

            foreach (ProjectObservation observation in result.Project.Observations)
            {
                Observations.Add(observation.Description);
            }

            //------------------------------------------
            // Scout Recommendations
            //------------------------------------------

            Recommendations.Clear();

            NextStep = "Waiting for Scout...";

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
        }
    }
}