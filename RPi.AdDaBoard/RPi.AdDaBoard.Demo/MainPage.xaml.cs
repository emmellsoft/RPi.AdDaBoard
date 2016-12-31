using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Emmellsoft.IoT.Rpi.AdDaBoard.Demo
{
    /// <summary>
    /// The intention of this class is to show the basic usage of the IAdDaBoard and may serve as a playground for it.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly Func<IAdDaBoard, Task> _currentDemo;

        public MainPage()
        {
            InitializeComponent();

            //===============================================================
            // Choose demo here!
            // ------------------------------

            _currentDemo = OutputDemo;
            //_currentDemo = InputDemo;

            //===============================================================
            
            Task.Run(async () => await Demo()).ConfigureAwait(false);
        }

        private async Task Demo()
        {
            // (Technically the demo will never end, but in a different scenario the IAdDaBoard should be disposed,
            // therefor I chose tho get the AD/DA-board in a using-statement.)

            using (IAdDaBoard adDaBoard = await AdDaBoardFactory.GetAdDaBoard().ConfigureAwait(false))
            {
                await _currentDemo(adDaBoard).ConfigureAwait(false);
            }
        }

        private static async Task OutputDemo(IAdDaBoard adDaBoard)
        {
            int outputLevel = 0;     // This ('outputLevel') variable will from 0 to 100 and back to 0 repeatedly ...
            int outputLevelStep = 5; // ... taking a step of this ('outputLevelStep') value at the time.

            while (true)
            {
                double normalizedOutputLevel = outputLevel / 100.0;
                double invertedNormalizedOutputLevel = 1.0 - normalizedOutputLevel;

                adDaBoard.Output.SetOutput(OutputPin.Output0, normalizedOutputLevel);
                adDaBoard.Output.SetOutput(OutputPin.Output1, invertedNormalizedOutputLevel);

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
            adDaBoard.Input.DataRate = InputDataRate.SampleRate50Sps;
            adDaBoard.Input.AutoSelfCalibrate = true;
            adDaBoard.Input.DetectCurrentSources = InputDetectCurrentSources.Off;
            adDaBoard.Input.Gain = InputGain.Gain1;

            // The demo continously reads the value of the 10 kohm potentiometer knob and the photo resistor and
            // writes the values to the Output window in Visual Studio.

            while (true)
            {
                double knobValue = adDaBoard.Input.GetInput(InputPin.Input0);
                double photoResistorValue = adDaBoard.Input.GetInput(InputPin.Input0);

                Debug.WriteLine($"Knob: {knobValue:0.0000000}, Photo resistor: {photoResistorValue:0.000}");

                await Task.Delay(100);
            }
        }
    }
}
