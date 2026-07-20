using SmartRenamer.Capabilities;
using SmartRenamer.Guide;
using SmartRenamer.Infrastructure;
using SmartRenamer.Models;
using SmartRenamer.Models.Recommendations;
using SmartRenamer.Models.Rename;
using SmartRenamer.Services;
using SmartRenamer.ViewModels.Guide;
using SmartRenamer.ViewModels.Workspace;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace SmartRenamer.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private WorkflowResult? currentWorkflow;

        private readonly SmartRenamer.Guide.RecommendationBuilder recommendationBuilder = new();

        private readonly RenameService renameService = new();
        private readonly ScoutService scoutService = new();
        public ObservableCollection<RenameItem> Files { get; } = new();

        public GuideViewModel Guide { get; } = new();

        public PipelineViewModel Pipeline { get; } = new();

        public ProjectWorkspaceViewModel Workspace { get; } = new();

        public RelayCommand AddFilesCommand { get; }

        public RelayCommand PreviewCommand { get; }

        public RelayCommand RenameCommand { get; }

        public RelayCommand ExecuteRecommendationCommand { get; }

        public MainWindowViewModel()
        {
            AddFilesCommand = new RelayCommand(AddFiles);
            PreviewCommand = new RelayCommand(Preview);
            RenameCommand = new RelayCommand(Rename);

            ExecuteRecommendationCommand =
                new RelayCommand(ExecuteRecommendation);

            Pipeline.AddStep(
                new WorkflowStep(
                    new ChooseFolderStep()));

            Guide.ProjectCreated += Guide_ProjectCreated;
            Guide.PlanApproved += Guide_PlanApproved;
        }

        private void Guide_ProjectCreated(object? sender,
                                          WorkflowResult result)
        {
            currentWorkflow = result;

            Workspace.Load(result);

            Workspace.Recommendations.Clear();

            foreach (Recommendation recommendation
                in recommendationBuilder.Build(result))
            {
                Workspace.Recommendations.Add(recommendation);
            }
        }

        private void Guide_PlanApproved(object? sender, EventArgs e)
        {
            if (currentWorkflow == null)
            {
                MessageBox.Show(
                    "No active project.",
                    "Scout");

                return;
            }

            RenameResult result =
                scoutService.Execute(currentWorkflow);

            if (result.Success)
            {
                string message =
                    "Scout successfully created an organized copy of your project.\n\n" +
                    $"Files copied: {result.FilesRenamed:N0}\n\n";

                if (!string.IsNullOrWhiteSpace(result.OutputFolder))
                {
                    message +=
                        $"Location:\n{result.OutputFolder}\n\n";
                }

                message +=
                    "Your original files were not modified.";

                MessageBox.Show(
                    message,
                    "Organization Complete");

                ProjectWorkflow workflow = new();

                currentWorkflow =
                    workflow.Execute(currentWorkflow.Project);

                Workspace.Load(currentWorkflow);

                Workspace.Recommendations.Clear();

                foreach (Recommendation recommendation
                    in recommendationBuilder.Build(currentWorkflow))
                {
                    Workspace.Recommendations.Add(recommendation);
                }
            }
            else
            {
                MessageBox.Show(
                    $"Renamed {result.FilesRenamed:N0} file(s).\n\n" +
                    string.Join("\n", result.Errors),
                    "Rename Complete");
            }
        }

        private void ExecuteRecommendation(object? parameter)
        {
            if (parameter is not Recommendation recommendation)
                return;

            switch (recommendation.ActionId)
            {
                case "ReviewPreview":
                    MessageBox.Show(
                        $"Scout is ready to review {Workspace.RenameCount} proposed filename change(s).",
                        "Scout");
                    break;

                case "RenameFiles":
                    Guide_PlanApproved(this, EventArgs.Empty);
                    break;

                case "ExplainChanges":
                    MessageBox.Show(
                        "Explanation mode isn't implemented yet.",
                        "Scout");
                    break;

                default:
                    MessageBox.Show(
                        $"Unknown action: {recommendation.ActionId}",
                        "Scout");
                    break;
            }
        }

        private void AddFiles()
        {
        }

        private void Preview()
        {
        }

        private void Rename()
        {
        }
    }
}