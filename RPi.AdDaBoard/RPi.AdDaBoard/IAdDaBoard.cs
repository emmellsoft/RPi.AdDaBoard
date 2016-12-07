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
        IAdc Adc { get; }

        /// <summary>
        /// A digital-to-analog converter.
        /// </summary>
        IDac Dac { get; }
    }
}