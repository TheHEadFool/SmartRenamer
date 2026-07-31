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
        private readonly List<SmokeParticle> _smoke = new();

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

            foreach (SmokeParticle smoke in _smoke)
            {
                _canvas.Children.Remove(smoke.Visual);
            }

            _embers.Clear();
            _smoke.Clear();
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            double deltaTime = (now - _lastUpdate).TotalSeconds;

            _lastUpdate = now;

            SpawnSmoke();
            SpawnEmbers();

            UpdateSmoke(deltaTime);
            UpdateEmbers(deltaTime);
        }

        private void SpawnSmoke()
        {
            // About one puff every 3–5 frames
            if (_random.NextDouble() > 0.25)
                return;

            SmokeParticle smoke = new SmokeParticle(_random);

            smoke.SetPosition(
                70 + Random(-4, 4),
                103 + Random(-2, 2));

            _smoke.Add(smoke);

            // Smoke stays behind the flame and embers
            _canvas.Children.Insert(0, smoke.Visual);
        }

        private void SpawnEmbers()
        {
            if (_random.NextDouble() > 0.03)
                return;

            EmberParticle ember = new EmberParticle(_random);

            ember.SetPosition(
                70 + Random(-8, 8),
                115 + Random(-3, 3));

            _embers.Add(ember);

            _canvas.Children.Add(ember.Visual);
        }

        private void UpdateSmoke(double deltaTime)
        {
            for (int i = _smoke.Count - 1; i >= 0; i--)
            {
                SmokeParticle smoke = _smoke[i];

                smoke.Update(deltaTime);

                if (!smoke.IsDead)
                    continue;

                _canvas.Children.Remove(smoke.Visual);

                _smoke.RemoveAt(i);
            }
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