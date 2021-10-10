using NewWorldMinimap.Core.PositionDetector;
using System.Numerics;
using System.Threading;

namespace NewWorldMinimap.Core.PositionProvider
{
    public class SimpleThreadedPositionProvider : IPositionProvider
    {
        public SimpleThreadedPositionProvider(IPositionDetector positionDetector)
        {
            PositionDetector = positionDetector;
            scannerThread = new Thread(UpdateLoop);
            scannerThread.Start();
        }

        public IPositionDetector PositionDetector { get; }

        private Thread scannerThread;
        private PositionResult posData;

        public void UpdateLoop()
        {
            while (true)
            {
                posData = PositionDetector.GetPosition();
                Thread.Sleep(200);
            }
        }

        public bool GetPosition(out Vector2 position)
        {

            if (posData == null)
            {
                position = default;
                return false;
            }
            position = posData.Position;
            return posData.Successful;
        }
    }
}
