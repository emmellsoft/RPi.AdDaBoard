using System.Diagnostics;

namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    internal class Timing
    {
        private readonly Stopwatch _stopwatch;
        private readonly double ticksPerMicrosecond;
        public Timing()
        {
            ticksPerMicrosecond = Stopwatch.Frequency / 1000000;
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        public long CurrentMilliseconds => _stopwatch.ElapsedMilliseconds;

        public void WaitMicroseconds(long microSeconds)
        {
            long finalTick = (long)(_stopwatch.ElapsedTicks + microSeconds * ticksPerMicrosecond);
            while (_stopwatch.ElapsedTicks < finalTick)
            {
                // Tight loop.  :-/
            }
        }
    }
}