using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SmartRenamer.ViewModels.Guide;

namespace SmartRenamer.Controls
{
    public partial class ConversationPanel : UserControl
    {
        public ConversationPanel()
        {
            InitializeComponent();
            Loaded += ConversationPanel_Loaded;
        }

        private void ConversationPanel_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(InputBox);

            if (DataContext is GuideViewModel guide)
            {
                guide.Conversation.Messages.CollectionChanged += Messages_CollectionChanged;
            }
        }

        private void Messages_CollectionChanged(
     object? sender,
     NotifyCollectionChangedEventArgs e)
        {
            //---------------------------------------------------------
            // Conversation scrolling
            //
            // Preserve the user's reading position when they have
            // deliberately scrolled upward.
            //
            // If the user was already at the bottom when a new
            // message arrived, continue following the conversation.
            //
            // This changes presentation behavior only. It does not
            // change conversation state, message history, or the
            // Conversation Engine.
            //---------------------------------------------------------

            System.Windows.Controls.ScrollViewer? scrollViewer =
                FindVisualChild<System.Windows.Controls.ScrollViewer>(
                    ConversationList);

            bool wasAtBottom = true;

            if (scrollViewer != null)
            {
                const double bottomTolerance = 8.0;

                wasAtBottom =
                    scrollViewer.VerticalOffset >=
                    scrollViewer.ScrollableHeight - bottomTolerance;
            }

            Dispatcher.BeginInvoke(() =>
            {
                if (wasAtBottom && ConversationList.Items.Count > 0)
                {
                    ConversationList.ScrollIntoView(
                        ConversationList.Items[
                            ConversationList.Items.Count - 1]);
                }

                Keyboard.Focus(InputBox);
            });
        }

        //---------------------------------------------------------
        // Find the ScrollViewer used by the conversation ListBox.
        //
        // The ListBox owns the actual scrolling surface, so we
        // inspect its visual tree rather than changing the XAML.
        //---------------------------------------------------------

        private static T? FindVisualChild<T>(
            System.Windows.DependencyObject parent)
            where T : System.Windows.DependencyObject
        {
            if (parent == null)
                return null;

            int childCount =
                System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                System.Windows.DependencyObject child =
                    System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is T result)
                    return result;

                T? descendant =
                    FindVisualChild<T>(child);

                if (descendant != null)
                    return descendant;
            }

            return null;
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;

            if (DataContext is GuideViewModel guide)
            {
                if (guide.SendCommand.CanExecute(null))
                {
                    guide.SendCommand.Execute(null);
                }
            }

            Keyboard.Focus(InputBox);
        }
    }
}