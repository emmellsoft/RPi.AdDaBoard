using System.Diagnostics;

namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    internal class Timing
    {
        private readonly Stopwatch _stopwatch;

        public Timing()
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        public void WaitMicroseconds(long microSeconds)
        {
            long initialTick = _stopwatch.ElapsedTicks;
            double desiredTicks = microSeconds / 1000000.0 * Stopwatch.Frequency;
            long finalTick = (long)(initialTick + desiredTicks);
            while (_stopwatch.ElapsedTicks < finalTick)
            {
                // Tight loop.  :-/
            }
        }
    }
}