using System.Windows.Controls;

namespace Scout.Expeditions.Safari.Animation
{
    public class EmberSystem
    {
        private readonly Canvas _canvas;

        public EmberSystem(Canvas canvas)
        {
            _canvas = canvas;
        }

        public void Start()
        {
            var ember = new System.Windows.Shapes.Ellipse
            {
                Width = 4,
                Height = 4,
                Fill = System.Windows.Media.Brushes.OrangeRed,
                Opacity = 0.9
            };

            Canvas.SetLeft(ember, 80);
            Canvas.SetTop(ember, 120);

            _canvas.Children.Add(ember);
        }
    }
}