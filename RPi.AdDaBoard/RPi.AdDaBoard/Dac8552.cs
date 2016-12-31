using System;

namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    internal sealed class Dac8552 : IAnalogOutput, IDisposable
    {
        private const byte OutputPin0 = 0x30; // Magic number to access the DAC output outputPin Output0.
        private const byte OutputPin1 = 0x34; // Magic number to access the DAC output outputPin B.

        public Dac8552(ISpiComm spiComm)
        {
            SpiComm = spiComm;
        }

        public void Dispose()
        {
        }

        public ISpiComm SpiComm { get; }

        public void SetOutput(OutputPin outputPin, double normalizedOutputLevel) => SetOutput(outputPin, normalizedOutputLevel, 1.0);

        public void SetOutput(OutputPin outputPin, double voltage, double vRef)
        {
            byte channelByte;
            switch (outputPin)
            {
                case OutputPin.Output0:
                    channelByte = OutputPin0;
                    break;
                case OutputPin.Output1:
                    channelByte = OutputPin1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outputPin), outputPin, null);
            }

            if (voltage > vRef)
            {
                voltage = vRef;
            }
            else if (voltage < 0)
            {
                voltage = 0;
            }

            ushort volt16Bit = (ushort)(ushort.MaxValue * voltage / vRef);

            byte[] data =
            {
                channelByte,
                (byte)(volt16Bit >> 8),
                (byte)(volt16Bit & 0xff)
            };

            SpiComm.Operate(spiDevice => spiDevice.Write(data));
        }
    }
}