using System.Numerics;

namespace NewWorldMinimap.Core.PositionProvider
{
    public interface IPositionProvider
    {
        bool GetPosition(out Vector2 position);
    }
}
