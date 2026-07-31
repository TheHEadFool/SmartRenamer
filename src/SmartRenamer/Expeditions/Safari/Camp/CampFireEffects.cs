using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace Scout.Expeditions.Safari.Camp
{
    /// <summary>
    /// Controls all animated campfire particle effects.
    /// </summary>
    public sealed class CampFireEffects
    {
        private readonly Canvas _canvas;
        private readonly Random _random = new();

        private readonly List<EmberParticle> _embers = new();

        private DateTime _lastUpdate;

        public CampFireEffects(Canvas canvas)
        {
            _canvas = canvas;
            _lastUpdate = DateTime.Now;

            CompositionTarget.Rendering += OnRendering;
        }

        public void Dispose()
        {
            CompositionTarget.Rendering -= OnRendering;

            foreach (EmberParticle ember in _embers)
            {
                _canvas.Children.Remove(ember.Visual);
            }

            _embers.Clear();
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            double deltaTime = (now - _lastUpdate).TotalSeconds;

            _lastUpdate = now;

            SpawnEmbers();

            UpdateEmbers(deltaTime);
        }

        private void SpawnEmbers()
        {
            if (_random.NextDouble() > 0.04)
                return;

            EmberParticle ember = new EmberParticle(_random);

            ember.SetPosition(
                70 + Random(-10, 10),
                115 + Random(-4, 4));

            _embers.Add(ember);

            _canvas.Children.Add(ember.Visual);
        }

        private void UpdateEmbers(double deltaTime)
        {
            for (int i = _embers.Count - 1; i >= 0; i--)
            {
                EmberParticle ember = _embers[i];

                ember.Update(deltaTime);

                if (!ember.IsDead)
                    continue;

                _canvas.Children.Remove(ember.Visual);

                _embers.RemoveAt(i);
            }
        }

        private double Random(double min, double max)
        {
            return min + (_random.NextDouble() * (max - min));
        }
    }
}