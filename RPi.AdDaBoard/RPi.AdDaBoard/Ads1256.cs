using System;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;

namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    // TODO: Read registers at startup


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
                public const byte Sync = 0xFC;      // Synchronize the Output0/D Conversion  11111100 (FCh)
            }

            public static class Register
            {
                public const byte Status = 0x00;
                public const byte Mux = 0x01;
                public const byte AdControl = 0x02;
                public const byte SampleRate = 0x03;
            }

            public static class StatusValue
            {
                public const byte DataOutputBitOrderMostSignificantBitFirst = 0x00;
                public const byte DataOutputBitOrderLeastSignificantBitFirst = 0x08;

                public const byte AutoCalibrateDisabled = 0x00;
                public const byte AutoCalibrateEnabled = 0x04;

                public const byte AnalogInputBufferDisabled = 0x00;
                public const byte AnalogInputBufferEnabled = 0x02;
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

        private InputGain? _currentGain;
        private InputDataRate? _currentDataRate;
        private InputDetectCurrentSources? _currentDetectCurrentSources;
        private bool? _autoCalibrate;
        private bool? _useInputBuffer;
        private readonly GpioPin _dataReadyPin;
        private readonly Timing _timing = new Timing();
        private int _gainFactor = 1;

        public Ads1256(ISpiComm spiComm, int dataReadyPinNumber)
        {
            SpiComm = spiComm;

            _dataReadyPin = GpioController.GetDefault().OpenPin(dataReadyPinNumber);
            _dataReadyPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
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

                return rawInputValue * vRef / (0x7FFFFF * _gainFactor);
            });
        }

        private int ReadRawInputValue(SpiDevice spiDevice)
        {
            WaitDataReady();

            // The buffer to fit the sample of 24 bits (i.e. 3 bytes)
            byte[] raw24BitBuffer = new byte[3];

            spiDevice.Write(new[] { Constants.Command.ReadData });

            _timing.WaitMicroseconds(10);

            spiDevice.Read(raw24BitBuffer);

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
                case InputPin.Input0:
                    return Constants.MuxValue.PositiveInputChannel_AnalogIn0;
                case InputPin.Input1:
                    return Constants.MuxValue.PositiveInputChannel_AnalogIn1;
                case InputPin.Input2:
                    return Constants.MuxValue.PositiveInputChannel_AnalogIn2;
                case InputPin.Input3:
                    return Constants.MuxValue.PositiveInputChannel_AnalogIn3;
                case InputPin.Input4:
                    return Constants.MuxValue.PositiveInputChannel_AnalogIn4;
                case InputPin.Input5:
                    return Constants.MuxValue.PositiveInputChannel_AnalogIn5;
                case InputPin.Input6:
                    return Constants.MuxValue.PositiveInputChannel_AnalogIn6;
                case InputPin.Input7:
                    return Constants.MuxValue.PositiveInputChannel_AnalogIn7;
                default:
                    throw new ArgumentOutOfRangeException(nameof(inputPin), inputPin, null);
            }
        }

        private static byte GetNegativeInputMuxValue(InputPin inputPin)
        {
            switch (inputPin)
            {
                case InputPin.Input0:
                    return Constants.MuxValue.NegativeInputChannel_AnalogIn0;
                case InputPin.Input1:
                    return Constants.MuxValue.NegativeInputChannel_AnalogIn1;
                case InputPin.Input2:
                    return Constants.MuxValue.NegativeInputChannel_AnalogIn2;
                case InputPin.Input3:
                    return Constants.MuxValue.NegativeInputChannel_AnalogIn3;
                case InputPin.Input4:
                    return Constants.MuxValue.NegativeInputChannel_AnalogIn4;
                case InputPin.Input5:
                    return Constants.MuxValue.NegativeInputChannel_AnalogIn5;
                case InputPin.Input6:
                    return Constants.MuxValue.NegativeInputChannel_AnalogIn6;
                case InputPin.Input7:
                    return Constants.MuxValue.NegativeInputChannel_AnalogIn7;
                default:
                    throw new ArgumentOutOfRangeException(nameof(inputPin), inputPin, null);
            }
        }

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

        private static byte GetSampleRateByteValue(InputDataRate dataRate)
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

        private byte GetDetectCurrentSourcesByteValue(InputDetectCurrentSources detectCurrentSources)
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

        private static byte GetGainByteValue(InputGain gain)
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

        private void EnsureConfiguration(SpiDevice spiDevice)
        {
            if ((_currentGain == Gain) &&
                (_currentDataRate == DataRate) &&
                (_currentDetectCurrentSources == DetectCurrentSources) &&
                (_autoCalibrate == AutoSelfCalibrate) &&
                (_useInputBuffer == UseInputBuffer))
            {
                return;
            }

            _currentGain = Gain;
            _currentDataRate = DataRate;
            _currentDetectCurrentSources = DetectCurrentSources;
            _autoCalibrate = AutoSelfCalibrate;
            _useInputBuffer = UseInputBuffer;

            switch (_currentGain.Value)
            {
                case InputGain.Gain1:
                    _gainFactor = 1;
                    break;
                case InputGain.Gain2:
                    _gainFactor = 2;
                    break;
                case InputGain.Gain4:
                    _gainFactor = 4;
                    break;
                case InputGain.Gain8:
                    _gainFactor = 8;
                    break;
                case InputGain.Gain16:
                    _gainFactor = 16;
                    break;
                case InputGain.Gain32:
                    _gainFactor = 32;
                    break;
                case InputGain.Gain64:
                    _gainFactor = 64;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            WaitDataReady();

            byte statusRegister = (byte)(
                Constants.StatusValue.DataOutputBitOrderMostSignificantBitFirst |
                (_autoCalibrate.Value ? Constants.StatusValue.AutoCalibrateEnabled : Constants.StatusValue.AutoCalibrateDisabled) |
                (_useInputBuffer.Value ? Constants.StatusValue.AnalogInputBufferEnabled : Constants.StatusValue.AnalogInputBufferDisabled));

            byte muxRegister = Constants.MuxValue.PositiveInputChannel_AnalogIn0 | Constants.MuxValue.NegativeInputChannel_AnalogIn1; // The default value

            byte adControlRegisterGain = GetGainByteValue(_currentGain.Value);
            byte adControlRegisterDetectCurrentSourcesByte = GetDetectCurrentSourcesByteValue(_currentDetectCurrentSources.Value);
            byte adControlRegister = (byte)((0 << 5) | adControlRegisterDetectCurrentSourcesByte | adControlRegisterGain);

            byte sampleRateRegister = GetSampleRateByteValue(_currentDataRate.Value);

            byte[] spiData =
            {
                Constants.Command.WriteReg,
                0x03, // (The number of following bytes minus one)
                statusRegister,
                muxRegister,
                adControlRegister,
                sampleRateRegister
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