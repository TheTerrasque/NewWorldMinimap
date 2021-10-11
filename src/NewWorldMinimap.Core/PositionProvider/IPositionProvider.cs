using NewWorldMinimap.Core.PositionDetector;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;

namespace NewWorldMinimap.Core.PositionProvider
{
    public interface IPositionProvider
    {
        bool TryGetPosition(out Vector2 position);

        Image<Rgba32> DebugImage { get; }

        double ActorAngle { get; }

        void SetPositionDetector(IPositionDetector detector);
    }
}
