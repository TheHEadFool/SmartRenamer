using System.Collections.ObjectModel;
using SmartRenamer.Infrastructure;
using SmartRenamer.Models;
using SmartRenamer.Models.Analysis;

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

        public void Load(WorkflowResult result)
        {
            if (result == null)
                return;

            Title = result.Project.ProjectType;

            Description =
                $"Confidence: {result.Project.Confidence}%";

            Observations.Clear();

            foreach (ProjectObservation observation in result.Project.Observations)
            {
                Observations.Add(observation.Description);
            }

            Recommendations.Clear();

            if (result.Project.Profile != null)
            {
                foreach (Recommendation recommendation in result.Project.Profile.Recommendations)
                {
                    Recommendations.Add(recommendation);
                }

                if (Recommendations.Count > 0)
                {
                    NextStep = Recommendations[0].Title;
                }
                else
                {
                    NextStep = "Review Project";
                }
            }
            else
            {
                NextStep = "Review Project";
            }
        }
    }
}