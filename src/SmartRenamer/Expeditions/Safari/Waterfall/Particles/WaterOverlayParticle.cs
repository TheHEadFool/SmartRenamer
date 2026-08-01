using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Scout.Expeditions.Safari.Camp;

namespace Scout.Expeditions.Safari.Waterfall.Particles
{
    /// <summary>
    /// Temporary copy of SmokeParticle.
    /// This build verifies the waterfall particle system before
    /// any behavioral changes are made.
    /// </summary>
    public sealed class WaterOverlayParticle : Particle
    {
        private static readonly BitmapImage OverlayImage = new(
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

        public WaterOverlayParticle(Random random)
            : base(CreateImage())
        {
            _random = random;

            Image image = (Image)Visual;

            _rotation = new RotateTransform(Random(-10, 200));
            _scale = new ScaleTransform();

            _transformGroup = new TransformGroup();
            _transformGroup.Children.Add(_scale);
            _transformGroup.Children.Add(_rotation);

            image.RenderTransform = _transformGroup;
            image.RenderTransformOrigin = new Point(0.5, 0.5);

            // Shorter particles.
            double size = Random(18, 26);

            Width = size;
            Height = size;

            Lifetime = Random(5.0, 7.5);

            VelocityX = Random(-4.0, 4.0);
            VelocityY = Random(-18.0, -26.0);

            _growthRate = Random(2.5, 4.5);

            // Reduced sideways drift for water.
            _drift = Random(0.01, 0.04);

            _fadeStart = Lifetime * 0.15;

            Visual.Width = Width;
            Visual.Height = Height;

            // Brighter appearance.
            Visual.Opacity = Random(0.18, 0.30);

            UpdateScale();
        }

        public double Width { get; private set; }

        public double Height { get; private set; }

        public void SetPosition(double x, double y)
        {
            // Move farther to the right.
            X = x + 30;

            // Raise slightly.
            Y = y - 8;

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

            // Water falls instead of rising.
            Y -= VelocityY * deltaTime;

            Width += _growthRate * deltaTime;
            Height += _growthRate * deltaTime;

            Visual.Width = Width;
            Visual.Height = Height;

            UpdateScale();

            Canvas.SetLeft(Visual, X - Width / 2);
            Canvas.SetTop(Visual, Y - Height / 2);

            if (Y >= 110)
            {
                Age = Lifetime;
                return;
            }

            if (Age >= _fadeStart)
            {
                double fade =
                    1.0 -
                    ((Age - _fadeStart) /
                    (Lifetime - _fadeStart));

                Visual.Opacity =
                    Math.Max(0.0, fade * 0.30);
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
                Source = OverlayImage,
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