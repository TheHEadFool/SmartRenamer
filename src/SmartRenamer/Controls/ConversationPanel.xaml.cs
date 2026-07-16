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

        private void Messages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (ConversationList.Items.Count > 0)
                {
                    ConversationList.ScrollIntoView(
                        ConversationList.Items[ConversationList.Items.Count - 1]);
                }

                Keyboard.Focus(InputBox);
            });
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