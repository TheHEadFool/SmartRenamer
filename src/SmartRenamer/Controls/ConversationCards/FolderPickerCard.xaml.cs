using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartRenamer.Controls.ConversationCards
{
    public partial class FolderPickerCard : UserControl
    {
        public FolderPickerCard()
        {
            InitializeComponent();
        }

        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command),
                typeof(ICommand),
                typeof(FolderPickerCard));
    }
}