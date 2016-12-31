using System;

namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    /// <summary>
    /// An interface to the Raspberry Pi high-precision AD/DA board.
    /// </summary>
    public interface IAdDaBoard : IDisposable
    {
        /// <summary>
        /// An analog-to-digital converter.
        /// </summary>
        IAnalogInput Input { get; }

        /// <summary>
        /// Output0 digital-to-analog converter.
        /// </summary>
        IAnalogOutput Output { get; }
    }
}