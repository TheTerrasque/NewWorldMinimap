using System;
using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using NewWorldMinimap.Core.Util;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TesserNet;

namespace NewWorldMinimap.Core.PositionDetector
{
    /// <summary>
    /// Provides logic for performing OCR to find the position of the player.
    /// </summary>
    /// <seealso cref="IDisposable" />
    public class ImageTesseractCFPositionDetector : IDisposable, IPositionDetector
    {
        private const int XOffset = 277;
        private const int YOffset = 18;
        private const int TextWidth = XOffset;
        private const int TextHeight = YOffset;
        private const int MaxCounter = 5;
        private static Vector4 textColor = (Vector4)Color.FromRgb(220, 220, 160);

        private static readonly Regex PosRegex = new Regex(@"(\d+ \d+) (\d+ \d+)", RegexOptions.Compiled);

        private readonly ITesseract tesseract = new TesseractPool(new TesseractOptions
        {
            PageSegmentation = PageSegmentation.Line,
            Numeric = true,
            Whitelist = "[]0123456789 ,.",
        });

        private bool disposedValue;
        private float lastX;
        private float lastY;
        private int counter = int.MaxValue;

        public bool DebugEnabled { get; private set; }
        private IImageSource ImageSource { get;  set; }

        public ImageTesseractCFPositionDetector(IImageSource imageSource, bool debugEnabled = false)
        {
            DebugEnabled = debugEnabled;
            ImageSource = imageSource;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ImageTesseractCFPositionDetector"/> class.
        /// </summary>
        ~ImageTesseractCFPositionDetector()
            => Dispose(false);

        /// <summary>
        /// Tries to get the position from the image provider.
        /// </summary>
        /// <param name="bmp">The image.</param>
        /// <param name="position">The position.</param>
        /// <param name="debugEnabled">Determines whether or not the debug functionality is enabled.</param>
        /// <param name="debugImage">The resulting debug image.</param>
        /// <returns>The found position.</returns>
        public PositionResult GetPosition()
        {
            var result = new PositionResult();
            var bmp = ImageSource.GetImage();

            bmp.Mutate(x => x
                .Crop(new Rectangle(bmp.Width - XOffset, YOffset, TextWidth, TextHeight))
                .Resize(TextWidth * 4, TextHeight * 4)
                .ColorFilter(textColor)
            );

            if (TryGetPositionInternal(bmp, out Vector2 position))
            {
                result.Successful = true;
                result.Position = position;
            }
            return result;
        }

        /// <summary>
        /// Resets the counter.
        /// </summary>
        public void ResetCounter()
            => counter = int.MaxValue;

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    tesseract.Dispose();
                }

                disposedValue = true;
            }
        }

        private bool TryGetPositionInternal(Image<Rgba32> bmp, out Vector2 position)
        {
            position = default;

            bmp.Metadata.HorizontalResolution = 300;
            bmp.Metadata.VerticalResolution = 300;

            string text = tesseract.Read(bmp).Trim();
            Console.WriteLine();
            Console.WriteLine("Read: " + text);
            if (true)
            {
                text = Regex.Replace(text, @"[^0-9]+", " ");
                text = Regex.Replace(text, @"\s+", " ").Trim();
                Match m = PosRegex.Match(text);

                if (m.Success)
                {
                    float x = float.Parse(m.Groups[1].Value.Replace(' ', '.'), CultureInfo.InvariantCulture);
                    float y = float.Parse(m.Groups[2].Value.Replace(' ', '.'), CultureInfo.InvariantCulture);

                    if (x >= 4468 && x <= 14260 && y >= 84 && y <= 9999)
                    {
                        lastX = x;
                        lastY = y;
                        position = new Vector2(x, y);
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
