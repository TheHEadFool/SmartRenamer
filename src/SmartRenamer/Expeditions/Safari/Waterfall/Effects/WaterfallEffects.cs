using System;
using System.Collections.Generic;
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

        private readonly List<WaterOverlayParticle> _particles = new();

        private double _spawnTimer;

        public WaterfallEffects(Canvas layer)
        {
            _layer = layer;
        }

        public void Update()
        {
            _spawnTimer += 0.016;

            if (_spawnTimer >= 0.10)
            {
                _spawnTimer = 0;

                WaterOverlayParticle particle =
                    new WaterOverlayParticle(_random);

                particle.SetPosition(
                    760 + (_random.NextDouble() * 20.0) - 10.0,
                    35);

                _layer.Children.Add(particle.Visual);

                _particles.Add(particle);
            }

            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                WaterOverlayParticle particle = _particles[i];

                particle.Update(0.016);

                if (particle.IsDead)
                {
                    _layer.Children.Remove(particle.Visual);
                    _particles.RemoveAt(i);
                }
            }
        }
    }
}