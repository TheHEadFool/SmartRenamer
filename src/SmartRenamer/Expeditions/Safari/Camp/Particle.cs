using System.Windows;

namespace Scout.Expeditions.Safari.Camp
{
    /// <summary>
    /// Base class for all campfire particle effects.
    /// </summary>
    public abstract class Particle
    {
        protected Particle(FrameworkElement visual)
        {
            Visual = visual;
        }

        /// <summary>
        /// The WPF shape displayed on the canvas.
        /// </summary>
        public FrameworkElement Visual { get; }

        /// <summary>
        /// Current X position.
        /// </summary>
        public double X { get; protected set; }

        /// <summary>
        /// Current Y position.
        /// </summary>
        public double Y { get; protected set; }

        /// <summary>
        /// Horizontal velocity.
        /// </summary>
        public double VelocityX { get; protected set; }

        /// <summary>
        /// Vertical velocity.
        /// </summary>
        public double VelocityY { get; protected set; }

        /// <summary>
        /// Current age in seconds.
        /// </summary>
        public double Age { get; protected set; }

        /// <summary>
        /// Lifetime in seconds.
        /// </summary>
        public double Lifetime { get; protected set; }

        /// <summary>
        /// Returns true when the particle has expired.
        /// </summary>
        public bool IsDead => Age >= Lifetime;

        /// <summary>
        /// Called once immediately after creation.
        /// </summary>
        public virtual void Initialize()
        {
        }

        /// <summary>
        /// Called every frame.
        /// </summary>
        public abstract void Update(double deltaTime);
    }
}