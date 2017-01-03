using System;

namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    internal sealed class Dac8552 : IAnalogOutput, IDisposable
    {
        private const byte LoadA = 0x10;
        private const byte LoadB = 0x20;
        private const byte SelectA = 0x00;
        private const byte SelectB = 0x04;

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
            byte control = LoadA | LoadB;

            switch (outputPin)
            {
                case OutputPin.DAC0:
                    control |= SelectA;
                    break;
                case OutputPin.DAC1:
                    control |= SelectB;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outputPin), outputPin, null);
            }

            ushort volt16Bit = Get16BitVoltage(voltage, vRef);

            byte[] data =
            {
                control,
                (byte)(volt16Bit >> 8),
                (byte)(volt16Bit & 0xff)
            };

            SpiComm.Operate(spiDevice => spiDevice.Write(data));
        }

        public void SetOutputs(double normalizedOutputLevel0, double normalizedOutputLevel1) => SetOutputs(normalizedOutputLevel0, normalizedOutputLevel1, 1.0);

        public void SetOutputs(double voltage0, double voltage1, double vRef)
        {
            ushort voltA16Bit = Get16BitVoltage(voltage0, vRef);

            byte[] dataA =
            {
                SelectA, // Don't "load" yet...
                (byte)(voltA16Bit >> 8),
                (byte)(voltA16Bit & 0xff)
            };

            ushort voltB16Bit = Get16BitVoltage(voltage1, vRef);

            byte[] dataB =
            {
                SelectB | LoadA | LoadB, // Now load both!
                (byte)(voltB16Bit >> 8),
                (byte)(voltB16Bit & 0xff)
            };

            SpiComm.Operate(spiDevice => spiDevice.Write(dataA));
            SpiComm.Operate(spiDevice => spiDevice.Write(dataB));
        }

        private static ushort Get16BitVoltage(double voltage, double vRef)
        {
            if (voltage > vRef)
            {
                voltage = vRef;
            }
            else if (voltage < 0)
            {
                voltage = 0;
            }

            return (ushort)(ushort.MaxValue * voltage / vRef);
        }
    }
}