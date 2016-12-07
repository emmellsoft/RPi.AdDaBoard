using System;

namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    internal sealed class Dac8552 : IDac, IDisposable
    {
        private const byte ChannelA = 0x30; // Magic number to access the DAC output channel A.
        private const byte ChannelB = 0x34; // Magic number to access the DAC output channel B.

        private readonly ISpiComm _spiComm;

        public Dac8552(ISpiComm spiComm)
        {
            _spiComm = spiComm;
        }

        public void Dispose()
        {
            _spiComm.Dispose();
        }

        public void SetOutput(DacChannel dacChannel, double normalizedOutputLevel) => SetOutput(dacChannel, normalizedOutputLevel, 1.0);

        public void SetOutput(DacChannel dacChannel, double voltage, double vRef)
        {
            byte channel;
            switch (dacChannel)
            {
                case DacChannel.A:
                    channel = ChannelA;
                    break;
                case DacChannel.B:
                    channel = ChannelB;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dacChannel), dacChannel, null);
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
                channel,
                (byte)(volt16Bit >> 8),
                (byte)(volt16Bit & 0xff)
            };

            _spiComm.Use(spiDevice => spiDevice.Write(data));
        }
    }
}