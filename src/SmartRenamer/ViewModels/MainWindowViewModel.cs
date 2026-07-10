using System.Collections.ObjectModel;
using System.IO;
using SmartRenamer.Infrastructure;
using SmartRenamer.Models;
using SmartRenamer.Services;

namespace SmartRenamer.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private readonly FileDialogService fileDialog = new();

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
            foreach (string file in fileDialog.PickFiles())
            {
                Files.Add(new RenameItem
                {
                    CurrentName = Path.GetFileName(file),
                    NewName = Path.GetFileName(file),
                    FullPath = file,
                    Status = "Ready"
                });
            }
        }

        private void Preview()
        {
            foreach (RenameItem item in Files)
            {
                item.NewName = item.CurrentName.Replace(
                    FindText,
                    ReplaceText);
            }
        }

        private void Rename()
        {
            foreach (RenameItem item in Files)
            {
                if (item.CurrentName == item.NewName)
                    continue;

                string folder = Path.GetDirectoryName(item.FullPath)!;

                string newPath = Path.Combine(folder, item.NewName);

                File.Move(item.FullPath, newPath);

                item.FullPath = newPath;
                item.CurrentName = item.NewName;
                item.Status = "Renamed";
            }
        }
    }
}