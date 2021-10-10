using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;

namespace NewWorldMinimap.Core.PositionDetector
{
    public class PositionResult
    {
        public bool Successful { get; set; } = false;
        public Vector2 Position { get; set; } = default;
        public Image<Rgba32> DebugImage { get; set; }
    }

    public interface IPositionDetector
    {
        PositionResult GetPosition();
    }
}
