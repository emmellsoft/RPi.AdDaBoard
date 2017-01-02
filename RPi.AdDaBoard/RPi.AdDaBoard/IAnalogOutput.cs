namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    /// <summary>
    /// A digital-to-analog converter.
    /// </summary>
    public interface IAnalogOutput
    {
        /// <summary>
        /// Set the analog output voltage of one of the output pins.
        /// </summary>
        /// <param name="outputPin">The output pin.</param>
        /// <param name="normalizedOutputLevel">The output level between 0.0 (minimum voltage) and 1.0 (maximum voltage).</param>
        void SetOutput(OutputPin outputPin, double normalizedOutputLevel);

        /// <summary>
        /// Set the analog output voltage of one of the output pins.
        /// </summary>
        /// <param name="outputPin">The output pin.</param>
        /// <param name="voltage">The voltage level between 0.0 and <paramref name="vRef"/>.</param>
        /// <param name="vRef">The voltage reference (usually 3.3 or 5 Volt).</param>
        void SetOutput(OutputPin outputPin, double voltage, double vRef);

        /// <summary>
        /// Set the analog output voltage on both pins at the same time.
        /// </summary>
        /// <param name="normalizedOutputLevel0">The output level for pin DAC0, between 0.0 (minimum voltage) and 1.0 (maximum voltage).</param>
        /// <param name="normalizedOutputLevel1">The output level for pin DAC1, between 0.0 (minimum voltage) and 1.0 (maximum voltage).</param>
        void SetOutputs(double normalizedOutputLevel0, double normalizedOutputLevel1);

        /// <summary>
        /// Set the analog output voltage on both pins at the same time.
        /// </summary>
        /// <param name="voltage0">The voltage level for pin DAC0, between 0.0 and <paramref name="vRef"/>.</param>
        /// <param name="voltage1">The voltage level for pin DAC1, between 0.0 and <paramref name="vRef"/>.</param>
        /// <param name="vRef">The voltage reference (usually 3.3 or 5 Volt).</param>
        void SetOutputs(double voltage0, double voltage1, double vRef);

        /// <summary>
        /// Get direct access to the SPI communcation.
        /// </summary>
        ISpiComm SpiComm { get; }
    }
}