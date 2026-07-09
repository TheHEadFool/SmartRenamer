using SmartRenamer.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace SmartRenamer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<RenameItem> Files { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (string file in dialog.FileNames)
                {
                    Files.Add(new RenameItem
                    {
                        CurrentName = System.IO.Path.GetFileName(file),
                        NewName = System.IO.Path.GetFileName(file),
                        FullPath = file,
                        Status = "Ready"
                    });
                }
            }
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            foreach (RenameItem item in Files)
            {
                item.NewName = item.CurrentName.Replace(
                    FindTextBox.Text,
                    ReplaceTextBox.Text);
            }
        }

        private void Rename_Click(object sender, RoutedEventArgs e)
        {
            foreach (RenameItem item in Files)
            {
                try
                {
                    string folder = System.IO.Path.GetDirectoryName(item.FullPath)!;

                    string newPath = System.IO.Path.Combine(folder, item.NewName);

                    if (System.IO.File.Exists(newPath))
                    {
                        item.Status = "File already exists";
                        continue;
                    }

                    System.IO.File.Move(item.FullPath, newPath);

                    item.FullPath = newPath;
                    item.CurrentName = item.NewName;
                    item.Status = "Renamed";
                }
                catch (Exception ex)
                {
                    item.Status = ex.Message;
                }
            }
        }
    }
}