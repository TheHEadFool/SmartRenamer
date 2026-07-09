using System.Collections.ObjectModel;
using SmartRenamer.Infrastructure;
using SmartRenamer.Models;

namespace SmartRenamer.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        public ObservableCollection<RenameItem> Files { get; } = new();
        private string findText = "";
        public string FindText
        {
            get => findText;
            set => SetProperty(ref findText, value);
        }

        private string replaceText = "";
        public string ReplaceText
        {
            get => replaceText;
            set => SetProperty(ref replaceText, value);
        }

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