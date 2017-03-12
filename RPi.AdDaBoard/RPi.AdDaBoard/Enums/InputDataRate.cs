namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    /// <summary>
    /// The data rate from 2.5 to 30000 samples/second.
    /// It controls both the sample rate and a digital filter.
    /// Read more about it in the data sheet!
    /// </summary>
    public enum InputDataRate
    {
        /// <summary>
        /// 2.5 samples / second
        /// </summary>
        SampleRate2_5Sps,

        /// <summary>
        /// 5 samples / second
        /// </summary>
        SampleRate5Sps,

        /// <summary>
        /// 10 samples / second
        /// </summary>
        SampleRate10Sps,

        /// <summary>
        /// 15 samples / second
        /// </summary>
        SampleRate15Sps,

        /// <summary>
        /// 25 samples / second
        /// </summary>
        SampleRate25Sps,

        /// <summary>
        /// 30 samples / second
        /// </summary>
        SampleRate30Sps,

        /// <summary>
        /// 50 samples / second
        /// </summary>
        SampleRate50Sps,

        /// <summary>
        /// 60 samples / second
        /// </summary>
        SampleRate60Sps,

        /// <summary>
        /// 100 samples / second
        /// </summary>
        SampleRate100Sps,

        /// <summary>
        /// 500 samples / second
        /// </summary>
        SampleRate500Sps,

        /// <summary>
        /// 1000 samples / second
        /// </summary>
        SampleRate1000Sps,

        /// <summary>
        /// 2000 samples / second
        /// </summary>
        SampleRate2000Sps,

        /// <summary>
        /// 3750 samples / second
        /// </summary>
        SampleRate3750Sps,

        /// <summary>
        /// 7500 samples / second
        /// </summary>
        SampleRate7500Sps,

        /// <summary>
        /// 15000 samples / second
        /// </summary>
        SampleRate15000Sps,

        /// <summary>
        /// 30000 samples / second
        /// </summary>
        SampleRate30000Sps
    }
}