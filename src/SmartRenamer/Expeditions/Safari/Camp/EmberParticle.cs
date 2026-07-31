using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Scout.Expeditions.Safari.Camp
{
    /// <summary>
    /// Represents a single glowing ember.
    /// The particle is responsible only for its own behaviour.
    /// </summary>
    public sealed class EmberParticle : Particle
    {
        private readonly Random _random;
        private readonly double _drift;
        private readonly double _fadeStart;

        public EmberParticle(Random random)
            : base(CreateVisual(random))
        {
            _random = random;

            bool rare = random.Next(100) == 0;

            Width = rare ? Random(4.0, 6.0) : Random(2.0, 4.0);

            Height = random.Next(8) == 0
                ? Width * 1.8
                : Width;

            Lifetime = rare
                ? Random(3.0, 4.0)
                : Random(2.0, 2.8);

            VelocityX = Random(-6.0, 6.0);

            VelocityY = rare
                ? Random(-95.0, -70.0)
                : Random(-60.0, -40.0);

            _drift = Random(0.5, 2.0);

            _fadeStart = Lifetime * 0.60;

            Visual.Width = Width;
            Visual.Height = Height;
        }

        public double Width { get; }

        public double Height { get; }

        /// <summary>
        /// Called by the engine immediately after creation.
        /// </summary>
        public void SetPosition(double x, double y)
        {
            X = x;
            Y = y;

            Canvas.SetLeft(Visual, X);
            Canvas.SetTop(Visual, Y);
        }

        public override void Update(double deltaTime)
        {
            Age += deltaTime;

            if (IsDead)
                return;

            VelocityX += Math.Sin(Age * 5.0) * _drift;

            X += VelocityX * deltaTime;
            Y += VelocityY * deltaTime;

            Canvas.SetLeft(Visual, X);
            Canvas.SetTop(Visual, Y);

            if (Age >= _fadeStart)
            {
                double fade =
                    1.0 -
                    ((Age - _fadeStart) /
                    (Lifetime - _fadeStart));

                Visual.Opacity = Math.Max(0.0, fade);
            }
        }

        private static Ellipse CreateVisual(Random random)
        {
            return new Ellipse
            {
                Width = 3,
                Height = 3,
                Fill = new SolidColorBrush(RandomColor(random)),
                Opacity = RandomOpacity(random),
                IsHitTestVisible = false
            };
        }

        private static Color RandomColor(Random random)
        {
            Color[] colors =
            {
                Color.FromRgb(255,140,30),
                Color.FromRgb(255,170,50),
                Color.FromRgb(255,190,70),
                Color.FromRgb(255,220,120)
            };

            return colors[random.Next(colors.Length)];
        }

        private static double RandomOpacity(Random random)
        {
            return 0.75 + (random.NextDouble() * 0.25);
        }

        private double Random(double min, double max)
        {
            return min + (_random.NextDouble() * (max - min));
        }
    }
}