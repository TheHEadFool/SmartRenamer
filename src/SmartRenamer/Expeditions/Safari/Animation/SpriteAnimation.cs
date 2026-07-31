using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Scout.Expeditions.Safari.Animation
{
    public class SpriteAnimation
    {
        private readonly Image _target;
        private readonly List<BitmapImage> _frames = new();
        private readonly DispatcherTimer _timer = new();

        private int _currentFrame;

        public int FramesPerSecond
        {
            get => 12;
        }

        public SpriteAnimation(Image target)
        {
            _target = target;

            _timer.Tick += OnTick;
        }

        private void OnTick(object? sender, EventArgs e)
        {
            if (_frames.Count == 0)
                return;

            _currentFrame++;

            if (_currentFrame >= _frames.Count)
                _currentFrame = 0;

            _target.Source = _frames[_currentFrame];
        }

        public void LoadFrames(string folder, string prefix, int frameCount)
        {
            _frames.Clear();

            for (int i = 1; i <= frameCount; i++)
            {
                var image = new BitmapImage();

                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(
    $"pack://application:,,,/Expeditions/Safari/Assets/{folder}/{prefix}{i}.png",
    UriKind.Absolute);
                image.EndInit();

                image.Freeze();

                _frames.Add(image);
            }

            if (_frames.Count > 0)
            {
                _target.Source = _frames[0];
            }
        }

        public void Start()
        {
            if (_frames.Count == 0)
                return;

            _currentFrame = 0;

            _target.Source = _frames[0];

            _timer.Interval = TimeSpan.FromMilliseconds(1000.0 / FramesPerSecond);

            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }
    }
}