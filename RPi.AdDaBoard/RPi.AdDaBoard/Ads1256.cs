using System;

namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    internal class Ads1256 : IAdc, IDisposable
    {
        private readonly ISpiComm _spiComm;

        public Ads1256(ISpiComm spiComm)
        {
            _spiComm = spiComm;
        }

        public void Dispose()
        {
            _spiComm.Dispose();
        }
    }
}