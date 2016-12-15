using System;
using Windows.Devices.Spi;

namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    internal sealed class AdDaBoard : IAdDaBoard
    {
        private readonly SpiCommController _spiCommController;
        private bool _isDisposed;

        public AdDaBoard(SpiDevice spiDevice)
        {
            _spiCommController = new SpiCommController(spiDevice);

            ISpiComm ads1256SpiComm = _spiCommController.Create(AdDaBoardPins.Ads1256SpiChipSelectPinNumber);
            ISpiComm dac8552SpiComm = _spiCommController.Create(AdDaBoardPins.Dac8552SpiChipSelectPinNumber);

            Input = new Ads1256(ads1256SpiComm, AdDaBoardPins.Ads1256DataReadyPinNumber);
            Output = new Dac8552(dac8552SpiComm);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            ((IDisposable)Input)?.Dispose();
            ((IDisposable)Output)?.Dispose();

            _spiCommController.Dispose();

            _isDisposed = true;
        }

        public IAnalogInput Input { get; }

        public IAnalogOutput Output { get; }
    }
}