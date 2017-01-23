using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

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

            //_currentDemo = OutputDemo;
            //_currentDemo = InputDemo;
            _currentDemo = InputOutputDemo;
            //_currentDemo = KnobToScreenDemo; // <-- Requires a monitor connected to the Raspberry Pi.

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
                double normalizedOutputLevel = outputLevel / 100.0; // Get a floating point value between 0.0 and 1.0...
                double invertedNormalizedOutputLevel = 1.0 - normalizedOutputLevel; // ...and the "inverted"; between 1.0 and 0.0

                // Set the two outputs one at a time (of course the "SetOutputs" method could also be used here).
                adDaBoard.Output.SetOutput(OutputPin.DAC0, normalizedOutputLevel);
                adDaBoard.Output.SetOutput(OutputPin.DAC1, invertedNormalizedOutputLevel);

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
            const double vRef = 5.0;

            adDaBoard.Input.DataRate = InputDataRate.SampleRate50Sps;
            adDaBoard.Input.AutoSelfCalibrate = true;
            adDaBoard.Input.DetectCurrentSources = InputDetectCurrentSources.Off;
            adDaBoard.Input.Gain = InputGain.Gain1;
            adDaBoard.Input.UseInputBuffer = false;

            // The demo continously reads the value of the 10 kohm potentiometer knob and the photo resistor and
            // writes the values to the Output window in Visual Studio.

            while (true)
            {
                double knobValue = adDaBoard.Input.GetInput(vRef, InputPin.AD0);
                double photoResistorValue = adDaBoard.Input.GetInput(vRef, InputPin.AD1);

                Debug.WriteLine($"Knob: {knobValue:0.0000}, Photo resistor: {photoResistorValue:0.0000}");

                await Task.Delay(100);
            }
        }

        private static async Task InputOutputDemo(IAdDaBoard adDaBoard)
        {
            adDaBoard.Input.DataRate = InputDataRate.SampleRate50Sps;
            adDaBoard.Input.AutoSelfCalibrate = true;
            adDaBoard.Input.DetectCurrentSources = InputDetectCurrentSources.Off;
            adDaBoard.Input.Gain = InputGain.Gain1;
            adDaBoard.Input.UseInputBuffer = false;

            // The demo continously reads the value of the 10 kohm potentiometer knob and sets the onboard LEDs to its value.
            // Turning the knob to its min/max positions will light up one LED and turn off the other (in the middle both will be shining, but not at full intensity).

            while (true)
            {
                // Get the normalized knob-value between -1.0 and 1.0 (which will actually be between 0.0 and 1.0 due to how the board is constructed):
                double normalizedKnobValue = adDaBoard.Input.GetInput(1.0, InputPin.AD0);

                // ...and the "inverted" value:
                double invertedNormalizedKnobValue = 1.0 - normalizedKnobValue;

                // Set both outputs at the same time:
                adDaBoard.Output.SetOutputs(normalizedKnobValue, invertedNormalizedKnobValue);

                await Task.Delay(10);
            }
        }

        private async Task KnobToScreenDemo(IAdDaBoard adDaBoard)
        {
            adDaBoard.Input.DataRate = InputDataRate.SampleRate50Sps;
            adDaBoard.Input.AutoSelfCalibrate = true;
            adDaBoard.Input.DetectCurrentSources = InputDetectCurrentSources.Off;
            adDaBoard.Input.Gain = InputGain.Gain1;
            adDaBoard.Input.UseInputBuffer = false;

            // The demo continously reads the value of the 10 kohm potentiometer knob and both writes the
            // voltage on the screen and converts the value to an angle, rotating a canvas-drawing on the screen.

            while (true)
            {
                // Get the normalized knob-value between 0.0 and 1.0:
                double normalizedKnobValue = adDaBoard.Input.GetInput(1.0, InputPin.AD0);

                // Convert the normalized knob value to an angle between -130 and 130 degrees.
                double angle = normalizedKnobValue * 260 - 130;

                // Convert the normalized knob value to a voltage between 0.0 and 5.0 V.
                double voltage = normalizedKnobValue * 5.0;

                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                    {
                        // Write the voltage.
                        KnobValue.Text = voltage.ToString("0.000") + " V";

                        // Rotate the canvas drawing an arrow.
                        ArrowCanvas.RenderTransform = new RotateTransform { CenterX = 0.5, CenterY = 0.5, Angle = angle };
                    });

                await Task.Delay(100);
            }
        }
    }
}
