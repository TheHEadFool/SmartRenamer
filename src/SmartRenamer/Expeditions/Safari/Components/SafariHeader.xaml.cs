using System.Windows;
using System.Windows.Controls;

namespace SmartRenamer.Expeditions.Safari.Components
{
    public partial class SafariHeader : UserControl
    {
        public SafariHeader()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(SafariHeader),
                new PropertyMetadata("Scout"));

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(
                nameof(Subtitle),
                typeof(string),
                typeof(SafariHeader),
                new PropertyMetadata("YOUR GUIDE ON THIS FILE SAFARI"));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }
    }
}