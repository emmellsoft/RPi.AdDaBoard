using System;
using System.Threading.Tasks;
using Windows.Devices.Spi;

namespace Emmellsoft.IoT.Rpi.AdDaBoard
{
    /// <summary>
    /// A factory class for getting access to the IAdDaBoard.
    /// </summary>
    public static class AdDaBoardFactory
    {
        private static readonly Task<IAdDaBoard> _getAdDaBoard = CreateAdDaBoard();

        /// <summary>
        /// Get a reference to the AdDaBoard interface.
        /// </summary>
        public static Task<IAdDaBoard> GetAdDaBoard()
        {
            return _getAdDaBoard;
        }

        private static async Task<IAdDaBoard> CreateAdDaBoard()
        {
            SpiDevice spiDevice = await CreateSpiDevice().ConfigureAwait(false);

            return new AdDaBoard(spiDevice);
        }

        private static async Task<SpiDevice> CreateSpiDevice()
        {
            SpiController spiController = await SpiController.GetDefaultAsync();

            var settings = new SpiConnectionSettings(0)
            {
                ClockFrequency = 400000, // TODO! Investigate this number!
                Mode = SpiMode.Mode1
            };

            return spiController.GetDevice(settings);
        }
    }
}