namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    /// <summary>
    /// An analog-to-digital converter.
    /// </summary>
    public interface IAnalogInput
    {
        /// <summary>
        /// For the best resolution, set the gain value to the highest possible setting.
        /// This will depend on the largest input signal to be measured.
        /// For example, if the largest signal to be measured is 1.0 V, the optimum gain value would be <see cref="InputGain.Gain4"/>, which gives a full-scale input voltage of 1.25 V.
        /// Higher gain values cannot be used since they cannot handle a 1.0 V input signal.
        /// </summary>
        InputGain Gain { get; set; }

        /// <summary>
        /// The data rate from 2.5 to 30000 samples/second.
        /// It controls both the sample rate and a digital filter.
        /// Read more about it in the data sheet!
        /// </summary>
        InputDataRate DataRate { get; set; }

        /// <summary>
        /// The <see cref="InputDetectCurrentSources"/> can be activated to verify the integrity of an external sensor supplying a signal.
        /// A shorted sensor produces a very small signal while an open-circuit sensor produces a very large signal.
        /// </summary>
        InputDetectCurrentSources DetectCurrentSources { get; set; }

        /// <summary>
        /// Automatically perform a self-calibration when reading the input after <see cref="Gain"/> or <see cref="DataRate"/> has changed.
        /// </summary>
        bool AutoSelfCalibrate { get; set; }

        /// <summary>
        /// Turn on or off the low-drift chopper-stabilized buffer that dramatically increases the input impedance presented by the AD-converter.
        /// </summary>
        bool UseInputBuffer { get; set; }

        /// <summary>
        /// Perform a self calibration. <seealso cref="AutoSelfCalibrate"/>
        /// </summary>
        void PerformSelfCalibration();

        /// <summary>
        /// Read the value from the given input pin.
        /// The returned value will be in the range of -vRef ... +vRef.
        /// </summary>
        /// <param name="vRef">The reference voltage.</param>
        /// <param name="inputPin">The input pin to read.</param>
        double GetInput(double vRef, InputPin inputPin);

        /// <summary>
        /// Get a differential reading between the <see cref="positiveInputPin"/> and <see cref="negativeInputPin"/>.
        /// The returned value will be in the range of -vRef ... +vRef.
        /// </summary>
        /// <param name="vRef">The reference voltage.</param>
        /// <param name="positiveInputPin">The positive pin to read.</param>
        /// <param name="negativeInputPin">The negative pin to read.</param>
        /// <returns></returns>
        double GetInputDifference(double vRef, InputPin positiveInputPin, InputPin negativeInputPin);
        
        /// <summary>
        /// Get direct access to the SPI communcation.
        /// </summary>
        ISpiComm SpiComm { get; }
    }
}