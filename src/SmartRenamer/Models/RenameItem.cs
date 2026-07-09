using System.ComponentModel;

namespace SmartRenamer.Models
{
    public class RenameItem : INotifyPropertyChanged
    {
        private string currentName = "";
        private string newName = "";
        private string status = "";
        private string fullPath = "";

        public string CurrentName
        {
            get => currentName;
            set
            {
                currentName = value;
                OnPropertyChanged(nameof(CurrentName));
            }
        }

        public string NewName
        {
            get => newName;
            set
            {
                newName = value;
                OnPropertyChanged(nameof(NewName));
            }
        }

        public string Status
        {
            get => status;
            set
            {
                status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public string FullPath
        {
            get => fullPath;
            set
            {
                fullPath = value;
                OnPropertyChanged(nameof(FullPath));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}