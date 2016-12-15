namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    /// <summary>
    /// An analog-to-digital converter.
    /// </summary>
    public interface IAnalogInput
    {
        InputGain Gain { get; set; }

        InputSampleRate SampleRate { get; set; }

        InputDetectCurrentSources DetectCurrentSources { get; set; }

        bool AutoCalibrate { get; set; }

        double GetInput(AnalogInput input);

        double GetInputDifference(AnalogInput positiveInput, AnalogInput negativeInput);
    }
}