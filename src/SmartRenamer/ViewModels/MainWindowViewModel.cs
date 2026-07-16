using System.Collections.ObjectModel;
using SmartRenamer.Capabilities;
using SmartRenamer.Infrastructure;
using SmartRenamer.Models;
using SmartRenamer.ViewModels.Guide;
using SmartRenamer.ViewModels.Workspace;

namespace SmartRenamer.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        public ObservableCollection<RenameItem> Files { get; } = new();

        public GuideViewModel Guide { get; } = new();

        public PipelineViewModel Pipeline { get; } = new();

        public ProjectWorkspaceViewModel Workspace { get; } = new();

        public RelayCommand AddFilesCommand { get; }

        public RelayCommand PreviewCommand { get; }

        public RelayCommand RenameCommand { get; }

        public MainWindowViewModel()
        {
            AddFilesCommand = new RelayCommand(AddFiles);
            PreviewCommand = new RelayCommand(Preview);
            RenameCommand = new RelayCommand(Rename);

            Pipeline.AddStep(
                new WorkflowStep(
                    new ChooseFolderStep()));

            Guide.ProjectCreated += Guide_ProjectCreated;
        }

        private void Guide_ProjectCreated(object? sender,
                                          WorkflowResult result)
        {
            Workspace.Load(result);
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