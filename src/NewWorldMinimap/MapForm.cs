﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using NewWorldMinimap.Caches;
using NewWorldMinimap.Core.PositionDetector;
using NewWorldMinimap.Core.PositionProvider;
using NewWorldMinimap.Core.Util;
using NewWorldMinimap.Util;
using NonInvasiveKeyboardHookLibrary;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace NewWorldMinimap
{
    /// <summary>
    /// Provides an interface for rendering the map.
    /// </summary>
    /// <seealso cref="Form" />
    public class MapForm : Form
    {
        private enum PositionDetectors
        {
            ColorFilter,
            Original,
        }

        private PositionDetectors ActivePositionDetector = PositionDetectors.ColorFilter;
        private IPositionProvider _positionProvider;
        private readonly PictureBox picture = new PictureBox();
        private readonly MapImageCache map = new MapImageCache();
        private readonly MarkerCache markers = new MarkerCache();
        private readonly IconCache icons = new IconCache();

        private readonly ContextMenu menu = new ContextMenu();
        private readonly MenuItem alwaysOnTopButton;
        private readonly List<MenuItem> screenItems = new List<MenuItem>();
        private readonly List<MenuItem> refreshDelayItems = new List<MenuItem>();
        private readonly List<MenuItem> _menuPositionSource = new List<MenuItem>();
        private readonly MenuItem debugButton;
        private readonly KeyboardHookManager keyboardHookManager = new KeyboardHookManager();
        private int currentScreen;
        private int refreshDelay;
        private bool debugEnabled;

        private Thread? scannerThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapForm"/> class.
        /// </summary>
        public MapForm()
        {
            alwaysOnTopButton = new MenuItem("Always-on-top", ToggleAlwaysOnTop, Shortcut.None);
            debugButton = new MenuItem("Debug", ToggleDebug, Shortcut.None);
            debugEnabled = false;
            
            InitializeComponent();
            UpdatePositionProvider();
            StartUpdateLoop();
        }

        private static Icon LoadIcon()
        {
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"NewWorldMinimap.Resources.icons.app.ico");
            return new Icon(stream);
        }

        private void SetName(Vector2 pos)
        {
            string name = $"CptWesley's Minimap {pos.ToString("0.000", CultureInfo.InvariantCulture)}";
            this.Name = name;
            this.Text = name;
        }

        private IPositionDetector GetPositionDetector()
        {
            var screenRect = ScreenGrabber.GetScreenRect(currentScreen);
            var imageSource = new Screenshotter(screenRect);
            if (ActivePositionDetector == PositionDetectors.ColorFilter)
            {
                return new ImageTesseractCFPositionDetector(imageSource, debugEnabled);
            }

            if (ActivePositionDetector == PositionDetectors.Original)
            {
                return new ImageTesseractOriginalPositionDetector(imageSource, debugEnabled);
            }

            return null;
        }

        private void UpdatePositionProvider()
        {
            var positionDetector = GetPositionDetector();
            if (positionDetector == null)
            {
                Log.Warning("Could not get position detector");
                return;
            }

            if (_positionProvider == null)
            {
                _positionProvider = new PredictingThreadedPositionProvider(positionDetector);
            } else
            {
                _positionProvider.SetPositionDetector(positionDetector);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(512, 512);
            SetName(Vector2.Zero);
            picture.SizeMode = PictureBoxSizeMode.CenterImage;
            this.Controls.Add(picture);
            this.ResumeLayout(false);
            this.Width = 512;
            this.Height = 512;
            markers.Populate(map);
            this.Resize += (s, e) => UpdateSize();
            this.FormClosed += OnClose;
            this.Icon = LoadIcon();
            BuildMenu();

            keyboardHookManager.Start();
            keyboardHookManager.RegisterHotkey(0x60, () =>
            {
                Console.WriteLine("NumPad0 detected");
                //SaveScreenshot();
            });
        }

        private void SetDetectorSource(int index, PositionDetectors detector)
        {
            ActivePositionDetector = detector;
            foreach (MenuItem mi in _menuPositionSource)
            {
                mi.Checked = false;
            }

            _menuPositionSource[index].Checked = true;
            UpdatePositionProvider();
        }

        private void BuildMenu()
        {
            this.ContextMenu = menu;
            this.picture.ContextMenu = menu;

            for (int i = 0; i < ScreenGrabber.ScreenCount; i++)
            {
                int ic = i;
                MenuItem item = new MenuItem($"Screen {ic}", (s, e) => SelectScreen(ic), Shortcut.None);
                screenItems.Add(item);
            }

            var detectors = (PositionDetectors[])Enum.GetValues(typeof(PositionDetectors));
            for (int i = 0;  i < detectors.Length; i++)
            {
                var detector = detectors[i];
                var localI = i;
                MenuItem item = new MenuItem($"Position Source: {detector}", (s, e) => SetDetectorSource(localI, detector), Shortcut.None);
                if (detector == ActivePositionDetector)
                {
                    item.Checked = true;
                }

                _menuPositionSource.Add(item);
            }

            CreateRefreshMenuItem(30);
            CreateRefreshMenuItem(60);
            CreateRefreshMenuItem(90);

            menu.MenuItems.Add(alwaysOnTopButton);
            menu.MenuItems.Add("-");
            menu.MenuItems.AddRange(screenItems.ToArray());
            menu.MenuItems.Add("-");
            menu.MenuItems.AddRange(refreshDelayItems.ToArray());
            menu.MenuItems.Add("-");
            menu.MenuItems.AddRange(_menuPositionSource.ToArray());
            //menu.MenuItems.Add("-");
            //menu.MenuItems.Add(debugButton);

            SelectScreen(ScreenGrabber.GetPrimaryScreenIndex());
            SelectRefreshDelay(1, 60);
        }

        private void CreateRefreshMenuItem(int delay)
        {
            int index = refreshDelayItems.Count;
            refreshDelayItems.Add(new MenuItem($"Refresh delay: {delay}ms.", (s, e) => SelectRefreshDelay(index, delay), Shortcut.None));
        }

        private void SelectScreen(int index)
        {
            screenItems[currentScreen].Checked = false;
            currentScreen = index;
            screenItems[index].Checked = true;
            UpdatePositionProvider();
        }

        private void SelectRefreshDelay(int index, int delay)
        {
            refreshDelay = delay;

            foreach (MenuItem mi in refreshDelayItems)
            {
                mi.Checked = false;
            }

            refreshDelayItems[index].Checked = true;
        }

        private void ToggleAlwaysOnTop(object sender, EventArgs e)
        {
            if (alwaysOnTopButton.Checked)
            {
                alwaysOnTopButton.Checked = false;
                this.TopMost = false;
            }
            else
            {
                alwaysOnTopButton.Checked = true;
                this.TopMost = true;
            }
        }

        private void ToggleDebug(object sender, EventArgs e)
        {
            if (debugButton.Checked)
            {
                debugButton.Checked = false;
                this.debugEnabled = false;
            }
            else
            {
                debugButton.Checked = true;
                this.debugEnabled = true;
            }

            UpdatePositionProvider();
        }

        private void UpdateSize()
        {
            if (picture.Width != Width)
            {
                picture.Width = Width;
            }

            if (picture.Height != Height)
            {
                picture.Height = Height;
            }
        }

        private void OnClose(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private Thread StartUpdateLoop()
        {
            scannerThread = new Thread(UpdateLoop);
            scannerThread.Start();
            return scannerThread;
        }

        private void UpdateLoop()
        {
            Stopwatch sw = new Stopwatch();

            int i = 0;
            while (true)
            {
                sw.Restart();

                if (_positionProvider.TryGetPosition(out Vector2 pos))
                {
                    Redraw(pos, _positionProvider.ActorAngle);
                }
                else
                {
                    Log.Debug("Read Failure");
                }

                if (_positionProvider.DebugImage != null)
                {
                    DrawDebugImage(_positionProvider.DebugImage);
                }

                i++;

                sw.Stop();
                long elapsed = sw.ElapsedMilliseconds;

                if (elapsed < refreshDelay)
                {
                    Thread.Sleep(refreshDelay - (int)elapsed);
                }
            }
        }

        private void Redraw(Vector2 pos, double rotationAngle)
        {
            using Image<Rgba32> baseMap = map.GetTileForCoordinate(pos.X, pos.Y, picture.Width, picture.Height);

            (int imageX, int imageY) = MapImageCache.ToMinimapCoordinate(pos.X, pos.Y, pos.X, pos.Y, picture.Width, picture.Height);

            (int tileX, int tileY) = MapImageCache.GetTileCoordinatesForCoordinate(pos.X, pos.Y);
            IEnumerable<Marker> visibleMarkers = markers.Get(tileX, tileY, picture.Width, picture.Height);

            baseMap.Mutate(c =>
            {
                using Image<Rgba32> playerTriangle = icons.Get("player").Clone();
                AffineTransformBuilder builder = new AffineTransformBuilder();

                builder.AppendRotationRadians((float)rotationAngle);
                playerTriangle.Mutate(x => x.Transform(builder));
                c.DrawIcon(playerTriangle, imageX, imageY);

                foreach (Marker marker in visibleMarkers)
                {
                    (int ix, int iy) = MapImageCache.ToMinimapCoordinate(pos.X, pos.Y, marker.X, marker.Y, picture.Width, picture.Height);
                    c.DrawIcon(icons.Get(marker), ix, iy);
                }
            });

            using Image<Rgba32> newMap = baseMap.Recenter(imageX, imageY);
            System.Drawing.Image prev = picture.Image;

            SafeInvoke(() =>
            {
                SetName(pos);
                picture.Image = newMap.ToBitmap();
                UpdateSize();
            });

            prev?.Dispose();
        }

        private void DrawDebugImage(Image<Rgba32> img)
        {
            img.Mutate(x => x.Resize(img.Width / 2, img.Height / 2));
            Bitmap bmp = img.ToBitmap();

            if (picture.Image is null)
            {
                picture.Image = bmp;
            }
            else
            {
                using Graphics g = Graphics.FromImage(picture.Image);
                g.DrawImage(bmp, 0, 0);
                img.Dispose();
                bmp.Dispose();
                picture.Image = picture.Image;
            }
        }

        private void SafeInvoke(Action act)
        {
            try
            {
                Invoke(act);
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
