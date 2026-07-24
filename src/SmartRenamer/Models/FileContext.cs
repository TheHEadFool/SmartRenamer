using System.Collections.Generic;

using SmartRenamer.Infrastructure;

namespace SmartRenamer.Models
{
    public class FileContext : ObservableObject
    {
        private string originalFullPath = "";
        public string OriginalFullPath
        {
            get => originalFullPath;
            set => SetProperty(ref originalFullPath, value);
        }

        private string originalName = "";
        public string OriginalName
        {
            get => originalName;
            set => SetProperty(ref originalName, value);
        }

        private string currentFullPath = "";
        public string CurrentFullPath
        {
            get => currentFullPath;
            set => SetProperty(ref currentFullPath, value);
        }

        private string currentName = "";
        public string CurrentName
        {
            get => currentName;
            set => SetProperty(ref currentName, value);
        }

        private string destinationFolder = "";
        public string DestinationFolder
        {
            get => destinationFolder;
            set => SetProperty(ref destinationFolder, value);
        }

        private string destinationName = "";
        public string DestinationName
        {
            get => destinationName;
            set => SetProperty(ref destinationName, value);
        }

        private string status = "";
        public string Status
        {
            get => status;
            set => SetProperty(ref status, value);
        }

        public Dictionary<string, object> Variables { get; } = new();

        private string extension = "";

        public string Extension
        {
            get => extension;
            set => SetProperty(ref extension, value);
        }

        private string parentFolder = "";

        public string ParentFolder
        {
            get => parentFolder;
            set => SetProperty(ref parentFolder, value);
        }

        private string relativeFolder = "";

        public string RelativeFolder
        {
            get => relativeFolder;
            set => SetProperty(ref relativeFolder, value);
        }
        public List<string> Warnings { get; } = new();

        public List<string> Errors { get; } = new();
    }
}