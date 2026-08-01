using System.Windows;

namespace Scout.Expeditions.Safari.Foundation
{
    public class ScenicRegion
    {
        public Rect OriginalBounds { get; }

        public ScenicRegion(double x, double y, double width, double height)
        {
            OriginalBounds = new Rect(x, y, width, height);
        }

        public Rect Scale(double scaleX, double scaleY)
        {
            return new Rect(
                OriginalBounds.X * scaleX,
                OriginalBounds.Y * scaleY,
                OriginalBounds.Width * scaleX,
                OriginalBounds.Height * scaleY);
        }
    }
}