using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;

namespace NewWorldMinimap.Core.PositionDetector
{
    public class PositionResult
    {
        public bool Successful { get; set; }
        public Vector2 Position { get; set; }
        public Image<Rgba32> DebugImage { get; set; }
    }

    public interface IPositionDetector
    {
        PositionResult GetPosition();
    }
}
