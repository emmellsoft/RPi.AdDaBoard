namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    /// <summary>
    /// For the best resolution, set the gain value to the highest possible setting.
    /// This will depend on the largest input signal to be measured.
    /// For example, if the largest signal to be measured is 1.0 V, the optimum gain value would be <see cref="InputGain.Gain4"/>, which gives a full-scale input voltage of 1.25 V.
    /// Higher gain values cannot be used since they cannot handle a 1.0 V input signal.
    /// </summary>
    public enum InputGain
    {
        /// <summary>
        /// Output0 gain amplification of 1; the full-scale input voltage Vin = ±5 V when Vref = 2.5 V.
        /// </summary>
        Gain1,

        /// <summary>
        /// Output0 gain amplification of 2; the full-scale input voltage Vin = ±2.5V V when Vref = 2.5 V.
        /// </summary>
        Gain2,

        /// <summary>
        /// Output0 gain amplification of 4; the full-scale input voltage Vin = ±1.25 V when Vref = 2.5 V.
        /// </summary>
        Gain4,

        /// <summary>
        /// Output0 gain amplification of 8; the full-scale input voltage Vin = ±0.625 V when Vref = 2.5 V.
        /// </summary>
        Gain8,

        /// <summary>
        /// Output0 gain amplification of 16; the full-scale input voltage Vin = ±312.5 mV when Vref = 2.5 V.
        /// </summary>
        Gain16,

        /// <summary>
        /// Output0 gain amplification of 32; the full-scale input voltage Vin = ±156.25 mV when Vref = 2.5 V.
        /// </summary>
        Gain32,

        /// <summary>
        /// Output0 gain amplification of 64; the full-scale input voltage Vin = ±78.125 mV when Vref = 2.5 V.
        /// </summary>
        Gain64
    }
}