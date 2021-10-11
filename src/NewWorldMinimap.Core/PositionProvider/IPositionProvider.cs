using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Numerics;

namespace NewWorldMinimap.Core.PositionProvider
{
    public interface IPositionProvider
    {
        bool GetPosition(out Vector2 position);
        Image<Rgba32> DebugImage { get; }
    }
}
