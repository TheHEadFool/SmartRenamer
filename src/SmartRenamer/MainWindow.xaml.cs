using System;
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

        // ListBox controls inside Live Report can mark the mouse-wheel event
        // handled even though their own scrolling is disabled. Handle the
        // tunnelling event at the outer report ScrollViewer so the report itself
        // always scrolls when the pointer is over any of its sections.
        private void LiveReportScrollViewer_PreviewMouseWheel(
            object sender,
            System.Windows.Input.MouseWheelEventArgs e)
        {
            if (sender is not ScrollViewer scrollViewer || e.Delta == 0)
                return;

            double targetOffset =
                scrollViewer.VerticalOffset - (e.Delta * 0.75);

            scrollViewer.ScrollToVerticalOffset(
                Math.Max(0, Math.Min(scrollViewer.ScrollableHeight, targetOffset)));

            e.Handled = true;
        }
    }
}