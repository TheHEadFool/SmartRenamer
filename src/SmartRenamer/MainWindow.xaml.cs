using System.Windows;
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