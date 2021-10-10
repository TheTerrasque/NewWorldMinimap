using System.Drawing;
using System.Drawing.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace NewWorldMinimap.Core.Util
{



    /// <summary>
    /// Provides logic for getting a screenshot of an image.
    /// </summary>
    public class Screenshotter: IImageSource
    {
       public Screenshotter(System.Drawing.Rectangle rectangle)
        {
            ScreenRectangle = rectangle;
        }

        public System.Drawing.Rectangle ScreenRectangle { get; }

        /// <summary>
        /// Takes the screenshot.
        /// </summary>
        /// <param name="screenIndex">Index of the screen.</param>
        /// <returns>The taken screenshot.</returns>
        public Image<Rgba32> GetImage()
        {

            using Bitmap bmp = new Bitmap(ScreenRectangle.Width, ScreenRectangle.Height, PixelFormat.Format32bppRgb);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(ScreenRectangle.X, ScreenRectangle.Y, 0, 0, ScreenRectangle.Size, CopyPixelOperation.SourceCopy);
            }

            return bmp.ToImageSharp();
        }
    }
}
