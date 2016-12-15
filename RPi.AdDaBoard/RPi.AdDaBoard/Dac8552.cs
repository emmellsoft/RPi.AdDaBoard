using System;

namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    internal sealed class Dac8552 : IAnalogOutput, IDisposable
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

        public void SetOutput(OutputChannel channel, double normalizedOutputLevel) => SetOutput(channel, normalizedOutputLevel, 1.0);

        public void SetOutput(OutputChannel channel, double voltage, double vRef)
        {
            byte channelByte;
            switch (channel)
            {
                case OutputChannel.A:
                    channelByte = ChannelA;
                    break;
                case OutputChannel.B:
                    channelByte = ChannelB;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
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

            _spiComm.Use(spiDevice => spiDevice.Write(data));
        }
    }
}