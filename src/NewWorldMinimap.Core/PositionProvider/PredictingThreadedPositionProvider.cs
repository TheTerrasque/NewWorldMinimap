using NewWorldMinimap.Core.PositionDetector;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;

namespace NewWorldMinimap.Core.PositionProvider
{
    public class PredictingThreadedPositionProvider : IPositionProvider
    {
        public PredictingThreadedPositionProvider(IPositionDetector positionDetector, int refreshMS = 600)
        {
            PositionDetector = positionDetector;
            scannerThread = new Thread(UpdateLoop);
            scannerThread.Start();
            RefreshMS = refreshMS;
        }

        private TimeSpan StaleTime = TimeSpan.FromSeconds(3);
        public IPositionDetector PositionDetector { get; }
        public int RefreshMS { get; private set; }
        public DateTime LastRead { get; private set; }
        public bool LastReadStatus { get; private set; }

        private Thread scannerThread;
        private Vector2 deltaPerMs;
        private PositionResult posData;

        public void UpdateLoop()
        {
            var sw = new Stopwatch();
            while (true)
            {
                sw.Restart();
                var myPosData = PositionDetector.GetPosition();
                if (myPosData.Successful) {
                    if (posData != null && posData.Successful)
                    {
                        var delta = myPosData.Position - posData.Position;
                        var deltaTime = (float)(DateTime.Now - LastRead).TotalMilliseconds;
                        deltaPerMs = delta / new Vector2(deltaTime, deltaTime);
                    }
                    posData = myPosData;
                    LastRead = DateTime.Now;
                }
                LastReadStatus = myPosData.Successful;
                sw.Stop();

                long elapsed = sw.ElapsedMilliseconds;

                if (elapsed < RefreshMS)
                {
                    Thread.Sleep(RefreshMS - (int)elapsed);
                }
            }
        }

        public Vector2 PredictPosition()
        {
            var diff = (float)(DateTime.Now - LastRead).TotalMilliseconds;
            return posData.Position + (deltaPerMs * new Vector2(diff, diff));
        }

        public bool GetPosition(out Vector2 position)
        {

            if (posData == null)
            {
                position = default;
                return false;
            }
            position = PredictPosition();
            return DateTime.Now - LastRead < StaleTime;
        }
    }
}
