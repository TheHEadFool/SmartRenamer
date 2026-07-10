using System.Collections.ObjectModel;
using SmartRenamer.Infrastructure;
using SmartRenamer.Models;
using SmartRenamer.ViewModels.Guide;

namespace SmartRenamer.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        public ObservableCollection<RenameItem> Files { get; } = new();

        public GuideViewModel Guide { get; } = new();

        public RelayCommand AddFilesCommand { get; }

        public RelayCommand PreviewCommand { get; }

        public RelayCommand RenameCommand { get; }

        public MainWindowViewModel()
        {
            AddFilesCommand = new RelayCommand(AddFiles);

            PreviewCommand = new RelayCommand(Preview);

            RenameCommand = new RelayCommand(Rename);
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