using System;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;

namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    internal class SpiCommController
    {
        private readonly SpiDevice _spiDevice;
        private readonly object _syncObj = new object();

        private class SpiComm : ISpiComm
        {
            private readonly SpiDevice _spiDevice;
            private readonly GpioPin _chipSelectGpioPin;
            private readonly object _syncObj;

            public SpiComm(SpiDevice spiDevice, GpioPin chipSelectGpioPin, object syncObj)
            {
                _spiDevice = spiDevice;
                _chipSelectGpioPin = chipSelectGpioPin;
                _syncObj = syncObj;

                _chipSelectGpioPin.Write(GpioPinValue.High);
            }

            public void Dispose()
            {
                _chipSelectGpioPin.Dispose();
            }

            public void Operate(Action<SpiDevice> spiAction)
            {
                Operate(spiDevice => { spiAction(spiDevice); return false; });
            }

            public T Operate<T>(Func<SpiDevice, T> spiAction)
            {
                T result;

                lock (_syncObj)
                {
                    _chipSelectGpioPin.Write(GpioPinValue.Low);

                    try
                    {
                        result = spiAction(_spiDevice);
                    }
                    finally
                    {
                        _chipSelectGpioPin.Write(GpioPinValue.High);
                    }
                }

                return result;
            }
        }

        public SpiCommController(SpiDevice spiDevice)
        {
            _spiDevice = spiDevice;
        }

        public void Dispose()
        {
            _spiDevice.Dispose();
        }

        public ISpiComm Create(int chipSelectPinNumber)
        {
            GpioPin chipSelectGpioPin = GpioController.GetDefault().OpenPin(chipSelectPinNumber);
            chipSelectGpioPin.SetDriveMode(GpioPinDriveMode.Output);
            return new SpiComm(_spiDevice, chipSelectGpioPin, _syncObj);
        }
    }
}