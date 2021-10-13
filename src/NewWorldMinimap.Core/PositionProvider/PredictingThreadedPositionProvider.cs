using NewWorldMinimap.Core.PositionDetector;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;

namespace NewWorldMinimap.Core.PositionProvider
{
    public class PredictingThreadedPositionProvider : IPositionProvider
    {
        public PredictingThreadedPositionProvider(IPositionDetector positionDetector, int refreshMS = 600, int failRefreshMS = 200)
        {
            PositionDetector = positionDetector;
            scannerThread = new Thread(ThreadUpdateLoop);
            scannerThread.Start();
            this.refreshMS = refreshMS;
            this.failRefreshMS = failRefreshMS;
        }

        public DateTime LastRead { get; private set; }

        public Image<Rgba32> DebugImage { get; private set; }

        public bool LastReadStatus { get; private set; }

        public double ActorAngle { get; private set; }

        public IPositionDetector PositionDetector { get; private set; }

        public int refreshMS { get; private set; }

        public int failRefreshMS { get; private set; }

        private TimeSpan staleTime = TimeSpan.FromSeconds(5);
        private Thread scannerThread;
        private Vector2 deltaPerMs;
        private PositionResult posData;

        public void SetRefreshMS(int milliseconds)
        {
            refreshMS = milliseconds;
        }

        public void SetFailRefreshMS(int milliseconds)
        {
            failRefreshMS = milliseconds;
        }

        private bool StaleData => (DateTime.Now - LastRead) > staleTime;

        public bool UpdatePosition()
        {
            var myPosData = PositionDetector.GetPosition();
            if (myPosData.Successful)
            {
                if (posData != null && posData.Successful)
                {
                    Vector2 delta;
                    if (StaleData) {
                        delta = Vector2.Zero;
                    } else {
                        delta = myPosData.Position - posData.Position;
                    }
                    if (delta != Vector2.Zero)
                    {
                        ActorAngle = Math.Atan2(delta.X, delta.Y);
                    }

                    var deltaTime = (float)(DateTime.Now - LastRead).TotalMilliseconds;
                    if (!StaleData)
                    {

                        if (delta.Length() > (0.0075 * deltaTime))
                        {
                            Serilog.Log.Information(
                                "Too much distance from {OldPos} and {NewPos}: {DistanceLength} - skipping",
                                posData.Position, myPosData.Position, delta.Length());
                            return false;
                        }
                    }

                    deltaPerMs = delta / new Vector2(deltaTime, deltaTime);
                }

                posData = myPosData;
                LastRead = DateTime.Now;
            }

            DebugImage = myPosData.DebugImage;
            return myPosData.Successful;
        }

        public void ThreadUpdateLoop()
        {
            var sw = new Stopwatch();
            while (true)
            {
                var refreshMS = this.refreshMS;
                sw.Restart();
                if (!UpdatePosition())
                {
                    refreshMS = failRefreshMS;
                }

                sw.Stop();

                long elapsed = sw.ElapsedMilliseconds;

                if (elapsed < refreshMS)
                {
                    Thread.Sleep(refreshMS - (int)elapsed);
                }
            }
        }

        public Vector2 PredictPosition()
        {
            var diff = (float)(DateTime.Now - LastRead).TotalMilliseconds;
            return posData.Position + (deltaPerMs * new Vector2(diff, diff));
        }

        public bool TryGetPosition(out Vector2 position)
        {

            if (posData == null)
            {
                position = default;
                return false;
            }

            position = PredictPosition();
            return !StaleData;
        }

        public void SetPositionDetector(IPositionDetector detector)
        {
            PositionDetector = detector;
        }
    }
}
