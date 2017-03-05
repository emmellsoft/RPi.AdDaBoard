namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    /// <summary>
    /// The <see cref="InputDetectCurrentSources"/> can be activated to verify the integrity of an external sensor supplying a signal.
    /// A shorted sensor produces a very small signal while an open-circuit sensor produces a very large signal.
    /// </summary>
    public enum InputDetectCurrentSources
    {
        /// <summary>
        /// No detection. (Default)
        /// </summary>
        Off,

        /// <summary>
        /// 0.5 µA
        /// </summary>
        Detect500NanoAmpere,

        /// <summary>
        /// 2 µA
        /// </summary>
        Detect2MicroAmpere,

        /// <summary>
        /// 10 µA
        /// </summary>
        Detect10MicroAmpere
    }
}