using System;
using Windows.Devices.Spi;

namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    /// <summary>
    /// SPI communication helper.
    /// </summary>
    public interface ISpiComm : IDisposable
    {
        /// <summary>
        /// Use the SPI connection.
        /// </summary>
        /// <param name="spiAction">Callback that gives access to the actual <see cref="SpiDevice"/>.</param>
        void Operate(Action<SpiDevice> spiAction);

        /// <summary>
        /// Use the SPI connection, returning a value.
        /// </summary>
        /// <param name="spiAction">Callback that gives access to the actual <see cref="SpiDevice"/>.</param>
        T Operate<T>(Func<SpiDevice, T> spiAction);
    }
}