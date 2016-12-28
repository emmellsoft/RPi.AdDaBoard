using System;
using Windows.Devices.Gpio;

namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    internal class Ads1256 : IAnalogInput, IDisposable
    {
        private static class Command
        {
            public const byte WakeUp = 0x00;        // Completes SYNC and Exits Standby Mode 00000000 (00h)
            public const byte ReadData = 0x01;      // Read Data                             00000001 (01h)
            public const byte ReadRegister = 0x10;  // Read from REG rrr                     0001rrrr (1xh)
            public const byte WriteRegister = 0x50; // Write to REG rrr                      0101rrrr (5xh)
            public const byte Sync = 0xFC;          // Synchronize the A/D Conversion        11111100 (FCh)
        }

        private static class Register
        {
            public const byte Status = 0x00;
            public const byte Mux = 0x01;
            public const byte AdControl = 0x02;
            public const byte SampleRate = 0x03;
        }

        private static class StatusValue
        {
            public const byte DataOutputBitOrderMostSignificantBitFirst = 0x00;
            public const byte DataOutputBitOrderLeastSignificantBitFirst = 0x08;

            public const byte AutoCalibrateDisabled = 0x00;
            public const byte AutoCalibrateEnabled = 0x04;

            public const byte AnalogInputBufferDisabled = 0x00;
            public const byte AnalogInputBufferEnable = 0x02;
        }

        private static class MuxValue
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

        private readonly ISpiComm _spiComm;
        private InputGain? _currentGain;
        private InputSampleRate? _currentSampleRate;
        private InputDetectCurrentSources? _currentDetectCurrentSources;
        private bool? _autoCalibrate;
        private readonly GpioPin _dataReadyPin;
        private readonly Timing _timing = new Timing();

        public Ads1256(ISpiComm spiComm, int dataReadyPinNumber)
        {
            _spiComm = spiComm;

            _dataReadyPin = GpioController.GetDefault().OpenPin(dataReadyPinNumber);
            _dataReadyPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
        }

        public void Dispose()
        {
            _spiComm.Dispose();
            _dataReadyPin.Dispose();
        }

        public InputGain Gain { get; set; }

        public InputSampleRate SampleRate { get; set; }

        public InputDetectCurrentSources DetectCurrentSources { get; set; }

        public bool AutoCalibrate { get; set; }

        public double GetInput(AnalogInput input)
        {
            return GetInput(
                GetPositiveInputMuxValue(input),
                MuxValue.NegativeInputChannel_AnalogInCom);
        }

        public double GetInputDifference(AnalogInput positiveInput, AnalogInput negativeInput)
        {
            return GetInput(
                GetPositiveInputMuxValue(positiveInput),
                GetNegativeInputMuxValue(negativeInput));
        }

        private double GetInput(byte positiveInputMuxValue, byte negativeInputMuxValue)
        {
            EnsureConfiguration();

            WriteRegister(Register.Mux, (byte)(positiveInputMuxValue | negativeInputMuxValue));
            _timing.WaitMicroseconds(5);

            WriteCommand(Command.Sync);
            _timing.WaitMicroseconds(5);

            WriteCommand(Command.WakeUp);
            _timing.WaitMicroseconds(25);

            int rawInputValue = ReadRawInputValue();

            return rawInputValue / 1000000.0;
        }

        private int ReadRawInputValue()
        {
            WaitDataReady();

            // The buffer to fit the sample of 24 bits (i.e. 3 bytes)
            byte[] raw24BitBuffer = new byte[3];

            _spiComm.Use(spiDevice =>
            {
                spiDevice.Write(new[] { Command.ReadData });

                _timing.WaitMicroseconds(10);

                spiDevice.Read(raw24BitBuffer);
            });

            uint value = ((uint)raw24BitBuffer[0] << 16) | ((uint)raw24BitBuffer[1] << 8) | raw24BitBuffer[2];

            // Convert a negative 24-bit value to a negative 32-bit value
            if ((raw24BitBuffer[0] & 0x80) != 0)
            {
                value |= 0xFF000000;
            }

            return unchecked((int)value);
        }

        private static byte GetPositiveInputMuxValue(AnalogInput input)
        {
            switch (input)
            {
                case AnalogInput.Input0:
                    return MuxValue.PositiveInputChannel_AnalogIn0;
                case AnalogInput.Input1:
                    return MuxValue.PositiveInputChannel_AnalogIn1;
                case AnalogInput.Input2:
                    return MuxValue.PositiveInputChannel_AnalogIn2;
                case AnalogInput.Input3:
                    return MuxValue.PositiveInputChannel_AnalogIn3;
                case AnalogInput.Input4:
                    return MuxValue.PositiveInputChannel_AnalogIn4;
                case AnalogInput.Input5:
                    return MuxValue.PositiveInputChannel_AnalogIn5;
                case AnalogInput.Input6:
                    return MuxValue.PositiveInputChannel_AnalogIn6;
                case AnalogInput.Input7:
                    return MuxValue.PositiveInputChannel_AnalogIn7;
                default:
                    throw new ArgumentOutOfRangeException(nameof(input), input, null);
            }
        }

        private static byte GetNegativeInputMuxValue(AnalogInput input)
        {
            switch (input)
            {
                case AnalogInput.Input0:
                    return MuxValue.NegativeInputChannel_AnalogIn0;
                case AnalogInput.Input1:
                    return MuxValue.NegativeInputChannel_AnalogIn1;
                case AnalogInput.Input2:
                    return MuxValue.NegativeInputChannel_AnalogIn2;
                case AnalogInput.Input3:
                    return MuxValue.NegativeInputChannel_AnalogIn3;
                case AnalogInput.Input4:
                    return MuxValue.NegativeInputChannel_AnalogIn4;
                case AnalogInput.Input5:
                    return MuxValue.NegativeInputChannel_AnalogIn5;
                case AnalogInput.Input6:
                    return MuxValue.NegativeInputChannel_AnalogIn6;
                case AnalogInput.Input7:
                    return MuxValue.NegativeInputChannel_AnalogIn7;
                default:
                    throw new ArgumentOutOfRangeException(nameof(input), input, null);
            }
        }

        private void WaitDataReady()
        {
            for (int i = 0; i < 400000; i++)
            {
                if (_dataReadyPin.Read() == GpioPinValue.Low)
                {
                    return;
                }
            }
        }

        private static byte GetSampleRateByteValue(InputSampleRate sampleRate)
        {
            switch (sampleRate)
            {
                case InputSampleRate.SampleRate30000Sps:
                    return 0xF0;
                case InputSampleRate.SampleRate15000Sps:
                    return 0xE0;
                case InputSampleRate.SampleRate7500Sps:
                    return 0xD0;
                case InputSampleRate.SampleRate3750Sps:
                    return 0xC0;
                case InputSampleRate.SampleRate2000Sps:
                    return 0xB0;
                case InputSampleRate.SampleRate1000Sps:
                    return 0xA1;
                case InputSampleRate.SampleRate500Sps:
                    return 0x92;
                case InputSampleRate.SampleRate100Sps:
                    return 0x82;
                case InputSampleRate.SampleRate60Sps:
                    return 0x72;
                case InputSampleRate.SampleRate50Sps:
                    return 0x63;
                case InputSampleRate.SampleRate30Sps:
                    return 0x53;
                case InputSampleRate.SampleRate25Sps:
                    return 0x43;
                case InputSampleRate.SampleRate15Sps:
                    return 0x33;
                case InputSampleRate.SampleRate10Sps:
                    return 0x23;
                case InputSampleRate.SampleRate5Sps:
                    return 0x13;
                case InputSampleRate.SampleRate2_5Sps:
                    return 0x03;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sampleRate), sampleRate, null);
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

        private void EnsureConfiguration()
        {
            if ((_currentGain == Gain) &&
                (_currentSampleRate == SampleRate) &&
                (_currentDetectCurrentSources == DetectCurrentSources) &&
                (_autoCalibrate == AutoCalibrate))
            {
                return;
            }

            _currentGain = Gain;
            _currentSampleRate = SampleRate;
            _currentDetectCurrentSources = DetectCurrentSources;
            _autoCalibrate = AutoCalibrate;

            WaitDataReady();

            byte statusRegister = (byte)(
                StatusValue.DataOutputBitOrderMostSignificantBitFirst |
                (_autoCalibrate.Value ? StatusValue.AutoCalibrateEnabled : StatusValue.AutoCalibrateDisabled) |
                StatusValue.AnalogInputBufferDisabled);

            byte muxRegister = MuxValue.PositiveInputChannel_AnalogIn0 | MuxValue.NegativeInputChannel_AnalogIn1; // The default value

            byte adControlRegisterGain = GetGainByteValue(_currentGain.Value);
            byte adControlRegisterDetectCurrentSourcesByte = GetDetectCurrentSourcesByteValue(_currentDetectCurrentSources.Value);
            byte adControlRegister = (byte)((0 << 5) | adControlRegisterDetectCurrentSourcesByte | adControlRegisterGain);

            byte sampleRateRegister = GetSampleRateByteValue(_currentSampleRate.Value);

            byte[] spiData =
            {
                Command.WriteRegister,
                0x03, // (The number of following bytes minus one)
                statusRegister,
                muxRegister,
                adControlRegister,
                sampleRateRegister
            };

            _spiComm.Use(spiDevice => spiDevice.Write(spiData));

            _timing.WaitMicroseconds(50);
        }

        private void WriteCommand(byte command)
        {
            byte[] spiData = { command };

            _spiComm.Use(spiDevice => spiDevice.Write(spiData));
        }

        private void WriteRegister(byte registerId, byte value)
        {
            byte[] spiData =
            {
                (byte)(Command.WriteRegister | registerId),
                0x00, // (The number of following bytes minus one)
                value
            };

            _spiComm.Use(spiDevice => spiDevice.Write(spiData));
        }
    }
}