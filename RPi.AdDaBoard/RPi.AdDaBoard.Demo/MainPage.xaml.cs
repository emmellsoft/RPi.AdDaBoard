using System.Diagnostics;
using System.Linq;
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
            // (Technically the demo will never end, but in a different scenario the IAdDaBoard should be disposed,
            // therefor I chose tho get the AD/DA-board in a using-statement.)

            using (IAdDaBoard adDaBoard = await AdDaBoardFactory.GetAdDaBoard().ConfigureAwait(false))
            {
                //await OutputDemo(adDaBoard).ConfigureAwait(false);
                await InputDemo(adDaBoard).ConfigureAwait(false);
            }
        }

        private static async Task OutputDemo(IAdDaBoard adDaBoard)
        {
            int outputLevel = 0;     // This value will from 0 to 100 and back to 0 repeatedly ...
            int outputLevelStep = 5; // ... taking a step of this value at the time.

            while (true)
            {
                double normalizedOutputLevel = outputLevel / 100.0;
                double invertedNormalizedOutputLevel = 1.0 - normalizedOutputLevel;

                adDaBoard.Output.SetOutput(OutputChannel.A, normalizedOutputLevel);
                adDaBoard.Output.SetOutput(OutputChannel.B, invertedNormalizedOutputLevel);

                outputLevel += outputLevelStep;
                if ((outputLevel == 0) || (outputLevel == 100))
                {
                    outputLevelStep = -outputLevelStep;
                }

                await Task.Delay(50);
            }
        }

        private static async Task InputDemo(IAdDaBoard adDaBoard)
        {
            adDaBoard.Input.SampleRate = InputSampleRate.SampleRate100Sps;
            adDaBoard.Input.AutoCalibrate = true;
            adDaBoard.Input.DetectCurrentSources = InputDetectCurrentSources.Detect500NanoAmpere;
            adDaBoard.Input.Gain = InputGain.Gain1;

            while (true)
            {
                var values = new[]
                {
                    adDaBoard.Input.GetInput(AnalogInput.Input0),
                    adDaBoard.Input.GetInput(AnalogInput.Input1),
                    adDaBoard.Input.GetInput(AnalogInput.Input2),
                    adDaBoard.Input.GetInput(AnalogInput.Input3),
                    adDaBoard.Input.GetInput(AnalogInput.Input4),
                    adDaBoard.Input.GetInput(AnalogInput.Input5),
                    adDaBoard.Input.GetInput(AnalogInput.Input6),
                    adDaBoard.Input.GetInput(AnalogInput.Input7)
                };

                Debug.WriteLine($"{string.Join("   ", values.Select(x => x.ToString("0.000")))}");

                await Task.Delay(100);
            }
        }
    }
}
