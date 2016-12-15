using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Emmellsoft.IoT.Rpi.AdDaBoard.Demo
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();

            Task.Run(async () => await Demo()).ConfigureAwait(false);
        }

        private static async Task Demo()
        {
            using (IAdDaBoard adDaBoard = await AdDaBoardFactory.GetAdDaBoard().ConfigureAwait(false))
            {
                int voltage = 0;
                int deltaVoltage = 5;

                for (int loop = 0; loop < 10000000; loop++)
                {
                    adDaBoard.Output.SetOutput(OutputChannel.A, voltage / 100.0);
                    adDaBoard.Output.SetOutput(OutputChannel.B, 1.0 - voltage / 100.0);

                    voltage += deltaVoltage;

                    if ((voltage == 0) || (voltage == 100))
                    {
                        deltaVoltage = -deltaVoltage;
                    }

                    await Task.Delay(50);
                }
            }
        }
    }
}
