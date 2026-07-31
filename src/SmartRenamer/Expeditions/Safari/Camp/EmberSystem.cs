using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Scout.Expeditions.Safari.Camp
{
    public class EmberSystem
    {
        private readonly Canvas _canvas;
        private readonly DispatcherTimer _timer = new();
        private readonly Random _random = new();

        public EmberSystem(Canvas canvas)
        {
            _canvas = canvas;

            _timer.Interval = TimeSpan.FromMilliseconds(900);
            _timer.Tick += (_, _) => SpawnEmber();
        }

        public void Start()
        {
            _timer.Start();
        }

        private void SpawnEmber()
        {
            bool highFlyer = _random.Next(100) == 0;

            double size = highFlyer
                ? _random.Next(4, 6)
                : _random.Next(2, 5);

            Color color = highFlyer
                ? Color.FromRgb(255, 245, 140)    // Bright yellow
                : Color.FromRgb(255, 180, 40);    // Normal ember orange

            double durationSeconds = highFlyer ? 3.0 : 2.5;

            var ember = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(color),
                Opacity = 0.9,
                IsHitTestVisible = false
            };

            double startX = 70 + _random.Next(-10, 11);
            double startY = 115;

            Canvas.SetLeft(ember, startX);
            Canvas.SetTop(ember, startY);

            _canvas.Children.Add(ember);

            double riseHeight = highFlyer
                ? _random.Next(140, 190)
                : _random.Next(40, 70);

            var duration = TimeSpan.FromSeconds(durationSeconds);

            var rise = new DoubleAnimation
            {
                From = startY,
                To = startY - riseHeight,
                Duration = duration,
                EasingFunction = new QuadraticEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            var drift = new DoubleAnimation
            {
                From = startX,
                To = startX + _random.Next(-20, 21),
                Duration = duration
            };

            var fade = new DoubleAnimation
            {
                From = 0.9,
                To = 0,
                Duration = duration
            };

            fade.Completed += (_, _) =>
            {
                _canvas.Children.Remove(ember);
            };

            ember.BeginAnimation(Canvas.TopProperty, rise);
            ember.BeginAnimation(Canvas.LeftProperty, drift);
            ember.BeginAnimation(UIElement.OpacityProperty, fade);
        }
    }
}