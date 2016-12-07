using System;

namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    internal sealed class AdDaBoard : IAdDaBoard
    {
        private const int Ads1256SpiChipSelectPinNumber = 22; // (GPIO 22)
        private const int Dac8552SpiChipSelectPinNumber = 23; // (GPIO 23)

        private readonly SpiCommFactory _spiCommFactory;
        private bool _isDisposed;

        public AdDaBoard(SpiCommFactory spiCommFactory)
        {
            _spiCommFactory = spiCommFactory;

            ISpiComm ads1256SpiComm = _spiCommFactory.Create(Ads1256SpiChipSelectPinNumber);
            ISpiComm dac8552SpiComm = _spiCommFactory.Create(Dac8552SpiChipSelectPinNumber);

            Adc = new Ads1256(ads1256SpiComm);
            Dac = new Dac8552(dac8552SpiComm);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            ((IDisposable)Adc)?.Dispose();
            ((IDisposable)Dac)?.Dispose();

            _spiCommFactory.Dispose();

            _isDisposed = true;
        }

        public IAdc Adc { get; }

        public IDac Dac { get; }
    }
}