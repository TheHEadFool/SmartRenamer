using System.Windows;
using System.Windows.Controls;

namespace Scout.Expeditions.Safari.Components
{
    public partial class SafariHeader : UserControl
    {
        public SafariHeader()
        {
            InitializeComponent();

            SizeChanged += SafariHeader_SizeChanged;
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
                new PropertyMetadata("Your Guide on the FIle Safari"));

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

        private void SafariHeader_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }
    }
}