using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NewWorldMinimap.Core.Util
{
    public interface IImageSource
    {
        Image<Rgba32> GetImage();
    }
}
