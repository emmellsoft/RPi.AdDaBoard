using System;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;

namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    internal class Ads1256 : IAnalogInput, IDisposable
    {
        private static class Constants
        {
            public static class Command
            {
                public const byte WakeUp = 0x00;    // Completes SYNC and Exits Standby Mode 00000000 (00h)
                public const byte ReadData = 0x01;  // Read Data                             00000001 (01h)
                public const byte ReadReg = 0x10;   // Read from REG rrr                     0001rrrr (1xh)
                public const byte WriteReg = 0x50;  // Write to REG rrr                      0101rrrr (5xh)
                public const byte SelfCal = 0xF0;   // Offset and Gain Self-Calibration      11110000 (F0h)
                public const byte Sync = 0xFC;      // Synchronize the A/D Conversion        11111100 (FCh)
            }

            public static class Register
            {
                public const byte Status = 0x00;
                public const byte Mux = 0x01;
                public const byte AdControl = 0x02;
                public const byte AdDataRate = 0x03;
            }

            public static class StatusRegister
            {
                public const byte AnalogInputBufferBit = 1;
                public const byte AutoCalibrateBit = 2;
                public const byte DataOutputBitOrderLeastSignificantBitFirstBit = 3;
            }

            public static class MuxValue
            {
                public const byte PositiveInputChannel_AnalogIn0 = 0x00; // Default
                public const byte PositiveInputChannel_AnalogIn1 = 0x10;
                public const byte PositiveInputChannel_AnalogIn2 = 0x20;
                public const byte PositiveInputChannel_AnalogIn3 = 0x30;
                public const byte PositiveInputChannel_AnalogIn4 = 0x40;
                public const byte PositiveInputChannel_AnalogIn5 = 0x50;
                public const byte PositiveInputChannel_AnalogIn6 = 0x60;
                public const byte PositiveInputChannel_AnalogIn7 = 0x70;
                public const byte PositiveInputChannel_AnalogInCom = 0x80;

                public const byte NegativeInputChannel_AnalogIn0 = 0x00;
                public const byte NegativeInputChannel_AnalogIn1 = 0x01; // Default
                public const byte NegativeInputChannel_AnalogIn2 = 0x02;
                public const byte NegativeInputChannel_AnalogIn3 = 0x03;
                public const byte NegativeInputChannel_AnalogIn4 = 0x04;
                public const byte NegativeInputChannel_AnalogIn5 = 0x05;
                public const byte NegativeInputChannel_AnalogIn6 = 0x06;
                public const byte NegativeInputChannel_AnalogIn7 = 0x07;
                public const byte NegativeInputChannel_AnalogInCom = 0x08;
            }
        }

        private class Registers
        {
            public Registers(
                InputGain gain,
                InputDataRate dataRate,
                InputDetectCurrentSources detectCurrentSources,
                bool autoCalibrate,
                bool useInputBuffer)
            {
                Gain = gain;
                DataRate = dataRate;
                DetectCurrentSources = detectCurrentSources;
                AutoCalibrate = autoCalibrate;
                UseInputBuffer = useInputBuffer;
                GainFactor = GetGainFactor();
            }

            public InputGain Gain { get; }

            public InputDataRate DataRate { get; }

            public InputDetectCurrentSources DetectCurrentSources { get; }

            public bool AutoCalibrate { get; }

            public bool UseInputBuffer { get; }

            public int GainFactor { get; }

            private int GetGainFactor()
            {
                switch (Gain)
                {
                    case InputGain.Gain1:
                        return 1;
                    case InputGain.Gain2:
                        return 2;
                    case InputGain.Gain4:
                        return 4;
                    case InputGain.Gain8:
                        return 8;
                    case InputGain.Gain16:
                        return 16;
                    case InputGain.Gain32:
                        return 32;
                    case InputGain.Gain64:
                        return 64;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private readonly GpioPin _dataReadyPin;
        private readonly Timing _timing = new Timing();
        private Registers _currentRegisters;
        private byte[] raw24BitBuffer = new byte[3];

        public Ads1256(ISpiComm spiComm, int dataReadyPinNumber)
        {
            SpiComm = spiComm;

            _dataReadyPin = GpioController.GetDefault().OpenPin(dataReadyPinNumber);
            _dataReadyPin.SetDriveMode(GpioPinDriveMode.InputPullUp);

            _currentRegisters = SpiComm.Operate(ReadRegisters);

            Gain = _currentRegisters.Gain;
            DataRate = _currentRegisters.DataRate;
            DetectCurrentSources = _currentRegisters.DetectCurrentSources;
            AutoSelfCalibrate = _currentRegisters.AutoCalibrate;
            UseInputBuffer = _currentRegisters.UseInputBuffer;
        }

        public void Dispose()
        {
            _dataReadyPin.Dispose();
        }

        public ISpiComm SpiComm { get; }

        public InputGain Gain { get; set; }

        public InputDataRate DataRate { get; set; }

        public InputDetectCurrentSources DetectCurrentSources { get; set; }

        public bool AutoSelfCalibrate { get; set; }

        public bool UseInputBuffer { get; set; }

        /// <summary>
        /// Waits for the "Data Ready" pin to go LOW.
        /// </summary>
        /// <param name="tightLoop">Wait in a tight loop (when expecting a really short wait time).</param>
        private void WaitDataReady(bool tightLoop = true)
        {
            long maxMilliseconds = _timing.CurrentMilliseconds + 1000;

            while ((_dataReadyPin.Read() == GpioPinValue.High) && (_timing.CurrentMilliseconds <= maxMilliseconds))
            {
                // Just wait...
                if (!tightLoop)
                {
                    Task.Delay(1).Wait();
                }
            }
        }

        public void PerformSelfCalibration()
        {
            SpiComm.Operate(spiDevice =>
            {
                WriteCommand(spiDevice, Constants.Command.SelfCal);

                WaitDataReady(false);
            });
        }

        public double GetInput(double vRef, InputPin inputPin)
        {
            return GetInput(
                vRef,
                GetPositiveInputMuxValue(inputPin),
                Constants.MuxValue.NegativeInputChannel_AnalogInCom);
        }

        public double GetInputDifference(double vRef, InputPin positiveInputPin, InputPin negativeInputPin)
        {
            return GetInput(
                vRef,
                GetPositiveInputMuxValue(positiveInputPin),
                GetNegativeInputMuxValue(negativeInputPin));
        }

        private double GetInput(double vRef, byte positiveInputMuxValue, byte negativeInputMuxValue)
        {
            return SpiComm.Operate(spiDevice =>
            {
                EnsureConfiguration(spiDevice);

                WriteRegister(spiDevice, Constants.Register.Mux, (byte)(positiveInputMuxValue | negativeInputMuxValue));
                _timing.WaitMicroseconds(5);

                WriteCommand(spiDevice, Constants.Command.Sync);
                _timing.WaitMicroseconds(5);

                WriteCommand(spiDevice, Constants.Command.WakeUp);
                _timing.WaitMicroseconds(25);

                int rawInputValue = ReadRawInputValue(spiDevice);

                return rawInputValue * vRef / (0x7FFFFF * _currentRegisters.GainFactor);
            });
        }

        private int ReadRawInputValue(SpiDevice spiDevice)
        {
            WaitDataReady();

            spiDevice.Write(new[] { Constants.Command.ReadData });

            _timing.WaitMicroseconds(10);

            spiDevice.Read(raw24BitBuffer);

            // thinking about using this instead
            //spiDevice.TransferSequential(new[] { Constants.Command.ReadData }, raw24BitBuffer);

            uint value = ((uint)raw24BitBuffer[0] << 16) | ((uint)raw24BitBuffer[1] << 8) | raw24BitBuffer[2];

            // Convert a negative 24-bit value to a negative 32-bit value
            if ((raw24BitBuffer[0] & 0x80) != 0)
            {
                value |= 0xFF000000;
            }

            return unchecked((int)value);
        }

        private static byte GetPositiveInputMuxValue(InputPin inputPin)
        {
            switch (inputPin)
            {
                case InputPin.AD0:
                    return Constants.MuxValue.PositiveInputChannel_AnalogIn0;
                case InputPin.AD1:
                    return Constants.MuxValue.PositiveInputChannel_AnalogIn1;
                case InputPin.AD2:
                    return Constants.MuxValue.PositiveInputChannel_AnalogIn2;
                case InputPin.AD3:
                    return Constants.MuxValue.PositiveInputChannel_AnalogIn3;
                case InputPin.AD4:
                    return Constants.MuxValue.PositiveInputChannel_AnalogIn4;
                case InputPin.AD5:
                    return Constants.MuxValue.PositiveInputChannel_AnalogIn5;
                case InputPin.AD6:
                    return Constants.MuxValue.PositiveInputChannel_AnalogIn6;
                case InputPin.AD7:
                    return Constants.MuxValue.PositiveInputChannel_AnalogIn7;
                default:
                    throw new ArgumentOutOfRangeException(nameof(inputPin), inputPin, null);
            }
        }

        private static byte GetNegativeInputMuxValue(InputPin inputPin)
        {
            switch (inputPin)
            {
                case InputPin.AD0:
                    return Constants.MuxValue.NegativeInputChannel_AnalogIn0;
                case InputPin.AD1:
                    return Constants.MuxValue.NegativeInputChannel_AnalogIn1;
                case InputPin.AD2:
                    return Constants.MuxValue.NegativeInputChannel_AnalogIn2;
                case InputPin.AD3:
                    return Constants.MuxValue.NegativeInputChannel_AnalogIn3;
                case InputPin.AD4:
                    return Constants.MuxValue.NegativeInputChannel_AnalogIn4;
                case InputPin.AD5:
                    return Constants.MuxValue.NegativeInputChannel_AnalogIn5;
                case InputPin.AD6:
                    return Constants.MuxValue.NegativeInputChannel_AnalogIn6;
                case InputPin.AD7:
                    return Constants.MuxValue.NegativeInputChannel_AnalogIn7;
                default:
                    throw new ArgumentOutOfRangeException(nameof(inputPin), inputPin, null);
            }
        }

        private static byte InputDataRateToByteValue(InputDataRate dataRate)
        {
            switch (dataRate)
            {
                case InputDataRate.SampleRate2_5Sps:
                    return 0x03;
                case InputDataRate.SampleRate5Sps:
                    return 0x13;
                case InputDataRate.SampleRate10Sps:
                    return 0x23;
                case InputDataRate.SampleRate15Sps:
                    return 0x33;
                case InputDataRate.SampleRate25Sps:
                    return 0x43;
                case InputDataRate.SampleRate30Sps:
                    return 0x53;
                case InputDataRate.SampleRate50Sps:
                    return 0x63;
                case InputDataRate.SampleRate60Sps:
                    return 0x72;
                case InputDataRate.SampleRate100Sps:
                    return 0x82;
                case InputDataRate.SampleRate500Sps:
                    return 0x92;
                case InputDataRate.SampleRate1000Sps:
                    return 0xA1;
                case InputDataRate.SampleRate2000Sps:
                    return 0xB0;
                case InputDataRate.SampleRate3750Sps:
                    return 0xC0;
                case InputDataRate.SampleRate7500Sps:
                    return 0xD0;
                case InputDataRate.SampleRate15000Sps:
                    return 0xE0;
                case InputDataRate.SampleRate30000Sps:
                    return 0xF0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataRate), dataRate, null);
            }
        }

        private static InputDataRate ByteValueToInputDataRate(byte value)
        {
            switch (value)
            {
                case 0x03:
                    return InputDataRate.SampleRate2_5Sps;
                case 0x13:
                    return InputDataRate.SampleRate5Sps;
                case 0x23:
                    return InputDataRate.SampleRate10Sps;
                case 0x33:
                    return InputDataRate.SampleRate15Sps;
                case 0x43:
                    return InputDataRate.SampleRate25Sps;
                case 0x53:
                    return InputDataRate.SampleRate30Sps;
                case 0x63:
                    return InputDataRate.SampleRate50Sps;
                case 0x72:
                    return InputDataRate.SampleRate60Sps;
                case 0x82:
                    return InputDataRate.SampleRate100Sps;
                case 0x92:
                    return InputDataRate.SampleRate500Sps;
                case 0xA1:
                    return InputDataRate.SampleRate1000Sps;
                case 0xB0:
                    return InputDataRate.SampleRate2000Sps;
                case 0xC0:
                    return InputDataRate.SampleRate3750Sps;
                case 0xD0:
                    return InputDataRate.SampleRate7500Sps;
                case 0xE0:
                    return InputDataRate.SampleRate15000Sps;
                case 0xF0:
                    return InputDataRate.SampleRate30000Sps;
                default:
                    // Should the value be invalid, us 30000 as default.
                    return InputDataRate.SampleRate30000Sps;
            }
        }

        private byte DetectCurrentSourcesToByteValue(InputDetectCurrentSources detectCurrentSources)
        {
            switch (detectCurrentSources)
            {
                case InputDetectCurrentSources.Off:
                    return 0x00;
                case InputDetectCurrentSources.Detect500NanoAmpere:
                    return 0x08;
                case InputDetectCurrentSources.Detect2MicroAmpere:
                    return 0x10;
                case InputDetectCurrentSources.Detect10MicroAmpere:
                    return 0x18;
                default:
                    throw new ArgumentOutOfRangeException(nameof(detectCurrentSources), detectCurrentSources, null);
            }
        }

        private InputDetectCurrentSources ByteValueToDetectCurrentSources(byte value)
        {
            // The "DetectCurrentSources" is determined by bit number 3 & 4 (counting from 0)
            switch (value)
            {
                case 0x00:
                    return InputDetectCurrentSources.Off;
                case 0x08:
                    return InputDetectCurrentSources.Detect500NanoAmpere;
                case 0x10:
                    return InputDetectCurrentSources.Detect2MicroAmpere;
                case 0x18:
                    return InputDetectCurrentSources.Detect10MicroAmpere;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private static byte InputGainToByteValue(InputGain gain)
        {
            switch (gain)
            {
                case InputGain.Gain1:
                    return 0;
                case InputGain.Gain2:
                    return 1;
                case InputGain.Gain4:
                    return 2;
                case InputGain.Gain8:
                    return 3;
                case InputGain.Gain16:
                    return 4;
                case InputGain.Gain32:
                    return 5;
                case InputGain.Gain64:
                    return 6;
                default:
                    throw new ArgumentOutOfRangeException(nameof(gain), gain, null);
            }
        }

        private static InputGain ByteValueToInputGain(byte value)
        {
            switch (value)
            {
                case 0:
                    return InputGain.Gain1;
                case 1:
                    return InputGain.Gain2;
                case 2:
                    return InputGain.Gain4;
                case 3:
                    return InputGain.Gain8;
                case 4:
                    return InputGain.Gain16;
                case 5:
                    return InputGain.Gain32;
                case 6:
                case 7:
                    return InputGain.Gain64;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }

        private void EnsureConfiguration(SpiDevice spiDevice)
        {
            if ((_currentRegisters.Gain == Gain) &&
                (_currentRegisters.DataRate == DataRate) &&
                (_currentRegisters.DetectCurrentSources == DetectCurrentSources) &&
                (_currentRegisters.AutoCalibrate == AutoSelfCalibrate) &&
                (_currentRegisters.UseInputBuffer == UseInputBuffer))
            {
                return;
            }

            _currentRegisters = new Registers(
                Gain,
                DataRate,
                DetectCurrentSources,
                AutoSelfCalibrate,
                UseInputBuffer);

            WriteRegisters(spiDevice, _currentRegisters);
        }

        private Registers ReadRegisters(SpiDevice spiDevice)
        {
            WaitDataReady();

            byte[] readRegResponse = new byte[4]; // Make room for Status, Mux, AdControl & AdDataRate

            byte[] readRegRequest =
            {
                Constants.Command.ReadReg,
                (byte)(readRegResponse.Length - 1)
            };

            spiDevice.Write(readRegRequest);

            _timing.WaitMicroseconds(10);

            spiDevice.Read(readRegResponse);

            byte statusRegister = readRegResponse[Constants.Register.Status];
            //byte muxRegister = readRegResponse[Constants.Register.Mux];  <-- The MUX value is not interesting
            byte adControlRegister = readRegResponse[Constants.Register.AdControl];
            byte adDataRateRegister = readRegResponse[Constants.Register.AdDataRate];

            bool autoCalibrate = (statusRegister & (1 << Constants.StatusRegister.AutoCalibrateBit)) != 0;
            bool useInputBuffer = (statusRegister & (1 << Constants.StatusRegister.AnalogInputBufferBit)) != 0;

            InputGain inputGain = ByteValueToInputGain((byte)(adControlRegister & 0x07)); // Bit 0-2 of the "AdControl" register holds the input gain value
            InputDetectCurrentSources inputDetectCurrentSources = ByteValueToDetectCurrentSources((byte)(adControlRegister & 0x18)); // Bit 3-4 of the "AdControl" register holds the "Detect Current Sources" value

            InputDataRate inputDataRate = ByteValueToInputDataRate(adDataRateRegister);

            return new Registers(
                inputGain,
                inputDataRate,
                inputDetectCurrentSources,
                autoCalibrate,
                useInputBuffer);
        }

        private void WriteRegisters(SpiDevice spiDevice, Registers registers)
        {
            WaitDataReady();

            const bool dataOutputBitOrderLeastSignificantBitFirst = false;

            byte statusRegister = (byte)((dataOutputBitOrderLeastSignificantBitFirst ? 1 << Constants.StatusRegister.DataOutputBitOrderLeastSignificantBitFirstBit : 0) |
                (registers.AutoCalibrate ? 1 << Constants.StatusRegister.AutoCalibrateBit : 0) |
                (registers.UseInputBuffer ? 1 << Constants.StatusRegister.AnalogInputBufferBit : 0));

            byte muxRegister = Constants.MuxValue.PositiveInputChannel_AnalogIn0 | Constants.MuxValue.NegativeInputChannel_AnalogIn1; // The default value

            byte adControlRegisterGain = InputGainToByteValue(registers.Gain);
            byte adControlRegisterDetectCurrentSourcesByte = DetectCurrentSourcesToByteValue(registers.DetectCurrentSources);
            byte adControlRegister = (byte)((0 << 5) | adControlRegisterDetectCurrentSourcesByte | adControlRegisterGain);

            byte adDataRateRegister = InputDataRateToByteValue(registers.DataRate);

            byte[] spiData =
            {
                Constants.Command.WriteReg,
                0x03, // (The number of following bytes minus one)
                statusRegister,
                muxRegister,
                adControlRegister,
                adDataRateRegister
            };

            spiDevice.Write(spiData);

            _timing.WaitMicroseconds(50);
        }

        private static void WriteCommand(SpiDevice spiDevice, byte command)
        {
            byte[] spiData = { command };

            spiDevice.Write(spiData);
        }

        private static void WriteRegister(SpiDevice spiDevice, byte registerId, byte value)
        {
            byte[] spiData =
            {
                (byte)(Constants.Command.WriteReg | registerId),
                0x00, // (The number of following bytes minus one)
                value
            };

            spiDevice.Write(spiData);
        }
    }
}