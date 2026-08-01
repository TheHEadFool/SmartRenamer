using System;
using System.Windows.Controls;
using Scout.Expeditions.Safari.Waterfall.Particles;

namespace Scout.Expeditions.Safari.Waterfall.Effects
{
    /// <summary>
    /// Coordinates all visual effects used by the waterfall.
    /// </summary>
    public class WaterfallEffects
    {
        private readonly Canvas _layer;

        private readonly Random _random = new();

        private readonly WaterOverlayParticle _fallParticle;

        private readonly WaterOverlayParticle _splashParticle;

        public WaterfallEffects(Canvas layer)
        {
            _layer = layer;

            _fallParticle = new WaterOverlayParticle(_random);
            _fallParticle.SetPosition(760, 35);

            _layer.Children.Add(_fallParticle.Visual);

            _splashParticle = new WaterOverlayParticle(_random);

            // Place it far to the right so it cannot overlap the main waterfall.
            _splashParticle.SetPosition(900, 60);

            _layer.Children.Add(_splashParticle.Visual);
        }

        public void Update()
        {
            _fallParticle.Update(0.016);

            _splashParticle.Update(0.016);
        }
    }
}