using System.Windows;
using System.Windows.Controls;
using SmartRenamer.Models;
using SmartRenamer.ViewModels;

namespace SmartRenamer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }

        
    }
}