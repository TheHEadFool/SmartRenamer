using System;

namespace Scout.Expeditions.Safari.Camp
{
    /// <summary>
    /// Controls the natural rhythm of the campfire.
    /// It does not know anything about particles or rendering.
    /// </summary>
    public sealed class CampFireBehavior
    {
        private readonly Random _random = new();

        private double _intensity = 0.45;
        private double _targetIntensity = 0.45;

        private double _nextMoodChange;
        private double _nextPop;

        public CampFireBehavior()
        {
            ScheduleMoodChange();
            ScheduleNextPop();
        }

        /// <summary>
        /// Current fire intensity.
        /// Range: 0.25 - 0.90
        /// </summary>
        public double Intensity => _intensity;

        /// <summary>
        /// Updates the fire rhythm.
        /// Returns true whenever the fire should "pop."
        /// </summary>
        public bool Update(double deltaTime)
        {
            _nextMoodChange -= deltaTime;
            _nextPop -= deltaTime;

            if (_nextMoodChange <= 0)
            {
                _targetIntensity = Random(0.25, 0.90);

                ScheduleMoodChange();
            }

            // Smoothly drift toward the new mood.
            _intensity +=
                (_targetIntensity - _intensity)
                * deltaTime
                * 0.30;

            if (_nextPop <= 0)
            {
                ScheduleNextPop();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Number of embers to emit during a flame pop.
        /// </summary>
        public int PopSize()
        {
            return _random.Next(2, 5);
        }

        private void ScheduleMoodChange()
        {
            _nextMoodChange = Random(12.0, 22.0);
        }

        private void ScheduleNextPop()
        {
            // Calm fires pop less often.
            double seconds =
                Random(3.0, 7.0) *
                (1.3 - _intensity);

            _nextPop = seconds;
        }

        private double Random(double min, double max)
        {
            return min +
                   (_random.NextDouble() * (max - min));
        }
    }
}