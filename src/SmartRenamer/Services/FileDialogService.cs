using Microsoft.Win32;
using System.Collections.Generic;

namespace SmartRenamer.Services
{
    public class FileDialogService
    {
        public IEnumerable<string> PickFiles()
        {
            OpenFileDialog dialog = new()
            {
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
                return dialog.FileNames;

            return new List<string>();
        }
    }
}