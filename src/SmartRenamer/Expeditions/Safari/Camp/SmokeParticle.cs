using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Scout.Expeditions.Safari.Camp
{
    /// <summary>
    /// Represents a single puff of campfire smoke.
    /// </summary>
    public sealed class SmokeParticle : Particle
    {
        private static readonly BitmapImage SmokeImage = new(
            new Uri(
                "pack://application:,,,/Expeditions/Safari/Assets/Fire4/Smoke.png",
                UriKind.Absolute));

        private readonly Random _random;

        private readonly RotateTransform _rotation;
        private readonly ScaleTransform _scale;
        private readonly TransformGroup _transformGroup;

        private readonly double _growthRate;
        private readonly double _drift;
        private readonly double _fadeStart;

        public SmokeParticle(Random random)
            : base(CreateImage())
        {
            _random = random;

            Image image = (Image)Visual;

            _rotation = new RotateTransform(Random(0, 360));
            _scale = new ScaleTransform();

            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(_scale);
            _transformGroup.Children.Add(_rotation);

            image.RenderTransform = _transformGroup;
            image.RenderTransformOrigin = new Point(0.5, 0.5);

            double size = Random(22, 38);

            Width = size;
            Height = size;

            Lifetime = Random(5.0, 7.5);

            VelocityX = Random(-4.0, 4.0);
            VelocityY = Random(-18.0, -26.0);

            _growthRate = Random(2.5, 4.5);

            _drift = Random(0.10, 0.45);

            _fadeStart = Lifetime * 0.15;

            Visual.Width = Width;
            Visual.Height = Height;

            Visual.Opacity = Random(0.10, 0.18);

            UpdateScale();
        }

        public double Width { get; private set; }

        public double Height { get; private set; }

        public void SetPosition(double x, double y)
        {
            X = x;
            Y = y;

            Canvas.SetLeft(Visual, X - Width / 2);
            Canvas.SetTop(Visual, Y - Height / 2);
        }

        public override void Update(double deltaTime)
        {
            Age += deltaTime;

            if (IsDead)
                return;

            VelocityX += Math.Sin(Age * 0.8) * _drift;

            X += VelocityX * deltaTime;
            Y += VelocityY * deltaTime;

            Width += _growthRate * deltaTime;
            Height += _growthRate * deltaTime;

            Visual.Width = Width;
            Visual.Height = Height;

            UpdateScale();

            Canvas.SetLeft(Visual, X - Width / 2);
            Canvas.SetTop(Visual, Y - Height / 2);

            if (Age >= _fadeStart)
            {
                double fade =
                    1.0 -
                    ((Age - _fadeStart) /
                    (Lifetime - _fadeStart));

                Visual.Opacity =
                    Math.Max(0.0, fade * 0.18);
            }
        }

        private void UpdateScale()
        {
            double scale = Width / 32.0;

            _scale.ScaleX = scale;
            _scale.ScaleY = scale;
        }

        private static Image CreateImage()
        {
            return new Image
            {
                Source = SmokeImage,
                Width = 32,
                Height = 32,
                Stretch = Stretch.Fill,
                IsHitTestVisible = false
            };
        }

        private double Random(double min, double max)
        {
            return min + (_random.NextDouble() * (max - min));
        }
    }
}