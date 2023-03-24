using Microsoft.IoT.Lightning.Providers;
using System;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace plant_monitoring_system_raspberry.Devices.Gpio
{
    /// <summary>
    /// Enum defineing the stepper motor drive modes.
    /// </summary>
    public enum DriveMode
    {
        FullStep,
        HalfStep,
        WaveDrive
    }

    /// <summary>
    /// A class that responsible for contronlling the stepper motor
    /// via driving the GPIO pins in a proper sequence.
    /// </summary>
    class StepperMotor : IStepperMotor, IDisposable
    {
        #region Fields
        private readonly DriveMode DRIVEMODE;
        private readonly byte MAX_STROKE;
        public readonly int MAX_POSITION;

        // GpioPin instances responsible for controlling the pins
        private GpioPin pinStepperInt1;
        private GpioPin pinStepperInt2;
        private GpioPin pinStepperInt3;
        private GpioPin pinStepperInt4;
        private GpioPin pinPhotointerrupter;
        #endregion

        #region Properties
        /// <value>
        /// Actual position of the stepper motor.
        /// </value>
        public int Position { get; private set; }

        /// <value>
        /// The actual stroke that's determining the actual driving pattern.
        /// </value>
        public byte Stroke { get; private set; }

        /// <value>
        /// The direction the motor will move during the stepping procedure.
        /// </value>
        public bool Direction { get; set; }
        public bool IsInitialized { get; private set; }
        #endregion

        /// <summary>
        /// Constructor that sets 3 thing. The drive mode, the maximal position and the number
        /// of strokes that the drivie mode determines.
        /// </summary>
        /// <param name="steps">
        /// The number of steps the concrete motor does per revolution.
        /// </param>
        /// <param name="gearRatio">
        /// The utilized gearings ratio.
        /// </param>
        /// <param name="mode">
        /// The drive mode that will be used for the motor.
        /// </param>
        public StepperMotor(int steps = 32, double gearRatio = 0.015625, DriveMode mode = DriveMode.WaveDrive)
        {
            var temp = (int)Math.Ceiling((double)steps / gearRatio);

            Position = 0;
            Stroke = 0;
            Direction = true;

            DRIVEMODE = mode;
            MAX_POSITION = ((mode == DriveMode.WaveDrive || mode == DriveMode.FullStep) ? temp : (temp * 2)) - 1;
            MAX_STROKE = (mode == DriveMode.WaveDrive || mode == DriveMode.FullStep) ? (byte)3 : (byte)7;
        }

        public async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("Stepper motor has already initialized.");
            }

            GpioController gpioController;

            if (LightningProvider.IsLightningEnabled)
            {
                gpioController = (await GpioController.GetControllersAsync(LightningGpioProvider.GetGpioProvider()))[0];
            }
            else
            {
                gpioController = GpioController.GetDefault();
            }

            if (gpioController == null)
            {
                throw new Exception("GPIO controller is unavailable on this device");
            }

            pinStepperInt1 = gpioController.OpenPin(5);
            pinStepperInt1.SetDriveMode(GpioPinDriveMode.Output);

            pinStepperInt2 = gpioController.OpenPin(6);
            pinStepperInt2.SetDriveMode(GpioPinDriveMode.Output);

            pinStepperInt3 = gpioController.OpenPin(13);
            pinStepperInt3.SetDriveMode(GpioPinDriveMode.Output);

            pinStepperInt4 = gpioController.OpenPin(19);
            pinStepperInt4.SetDriveMode(GpioPinDriveMode.Output);

            TurnAllPinOff();

            pinPhotointerrupter = gpioController.OpenPin(20);
            pinPhotointerrupter.SetDriveMode(GpioPinDriveMode.Input);

            IsInitialized = true;
        }

        /// <summary>
        /// Turns all pin of in order to stop the motor from heating.
        /// </summary>
        private void TurnAllPinOff()
        {
            pinStepperInt1.Write(GpioPinValue.Low);
            pinStepperInt2.Write(GpioPinValue.Low);
            pinStepperInt3.Write(GpioPinValue.Low);
            pinStepperInt4.Write(GpioPinValue.Low);
        }

        public async Task CalibrateAsync()
        {
            uint smoothness = 0;
            while (true)
            {
                smoothness += 100;
                Direction = true;
                for (int i = 0; i < smoothness; i++)
                {
                    if (pinPhotointerrupter.Read() == GpioPinValue.High)
                    {
                        Position = 0;
                        TurnAllPinOff();
                        return;
                    }
                    Step();
                    await Task.Delay(1);

                }
                Direction = false;
                for (int i = 0; i < 2 * smoothness; i++)
                {
                    if (pinPhotointerrupter.Read() == GpioPinValue.High)
                    {
                        Position = 0;
                        TurnAllPinOff();
                        return;
                    }
                    Step();
                    await Task.Delay(1);
                }
                Direction = true;
                for (int i = 0; i < smoothness; i++)
                {
                    Step();
                    await Task.Delay(1);
                }
                smoothness += smoothness;
            }
            throw new Exception("Program is unable to calibrate the stepper motor.");
        }

        public async Task MoveToPositionAsync(uint targetPosition)
        {
            // Ha nem változott a pozíció akkor ne mozogjon
            if (Position == targetPosition)
                return;
            // Irányválasztó határ meghatározása
            var half_position = (double)MAX_POSITION / 2;

            if (((targetPosition <= half_position) && (Position <= half_position)) ||
                ((targetPosition > half_position) && (Position > half_position))) // Ha az "északi" vagy a "déli" oldalon van a cél és a jelenlegi pozíció is
            {
                if (Position < targetPosition)
                    Direction = true;
                else
                    Direction = false;
            }
            else if (targetPosition <= half_position && Position > half_position) // Ha az "északi" oldalon van a cél, de a "délin" a jelenlegi
            {
                Direction = true;
            }
            else if (targetPosition > half_position && Position <= half_position) // Ha a "déli" oldalon van a cél, de az "északin" a jelenlegi
            {
                Direction = false;
            }

            // Execute the movement
            while (targetPosition != Position)
            {
                Step();
                await Task.Delay(1);
            }
            TurnAllPinOff();
        }

        public async Task MoveToAngleAsync(double angle)
        {
            // Convert angle parameter to be in [0,360] interval
            double temp = angle;
            if (temp < 0)
            {
                while (temp <= -360)
                {
                    temp += 360;
                }
                temp = 360 - temp;
            }
            else
            {
                while (temp >= 360)
                {
                    temp -= 360;
                }
            }
            double realAngle = temp;
            var targetPosition = (uint)Math.Round((realAngle * MAX_POSITION / 360));
            await MoveToPositionAsync(targetPosition);
        }

        private void Step()
        {
            if (Direction)
            {
                if (Stroke == MAX_STROKE)
                    Stroke = 0;
                else
                    ++Stroke;

                if (Position == MAX_POSITION)
                    Position = 0;
                else
                    ++Position;
            }
            else
            {
                if (Stroke == 0)
                    Stroke = MAX_STROKE;
                else
                    --Stroke;

                if (Position == 0)
                    Position = MAX_POSITION;
                else
                    --Position;
            }

            if (DRIVEMODE == DriveMode.WaveDrive)
            {
                switch (Stroke)
                {
                    case 0:
                        pinStepperInt1.Write(GpioPinValue.High);
                        pinStepperInt2.Write(GpioPinValue.Low);
                        pinStepperInt3.Write(GpioPinValue.Low);
                        pinStepperInt4.Write(GpioPinValue.Low);
                        break;
                    case 1:
                        pinStepperInt1.Write(GpioPinValue.Low);
                        pinStepperInt2.Write(GpioPinValue.High);
                        pinStepperInt3.Write(GpioPinValue.Low);
                        pinStepperInt4.Write(GpioPinValue.Low);
                        break;
                    case 2:
                        pinStepperInt1.Write(GpioPinValue.Low);
                        pinStepperInt2.Write(GpioPinValue.Low);
                        pinStepperInt3.Write(GpioPinValue.High);
                        pinStepperInt4.Write(GpioPinValue.Low);
                        break;
                    case 3:
                        pinStepperInt1.Write(GpioPinValue.Low);
                        pinStepperInt2.Write(GpioPinValue.Low);
                        pinStepperInt3.Write(GpioPinValue.Low);
                        pinStepperInt4.Write(GpioPinValue.High);
                        break;
                }
            }
            else if (DRIVEMODE == DriveMode.HalfStep)
            {
                switch (Stroke)
                {
                    case 0:
                        pinStepperInt1.Write(GpioPinValue.High);
                        pinStepperInt2.Write(GpioPinValue.Low);
                        pinStepperInt3.Write(GpioPinValue.Low);
                        pinStepperInt4.Write(GpioPinValue.Low);
                        break;
                    case 1:
                        pinStepperInt1.Write(GpioPinValue.High);
                        pinStepperInt2.Write(GpioPinValue.High);
                        pinStepperInt3.Write(GpioPinValue.Low);
                        pinStepperInt4.Write(GpioPinValue.Low);
                        break;
                    case 2:
                        pinStepperInt1.Write(GpioPinValue.Low);
                        pinStepperInt2.Write(GpioPinValue.High);
                        pinStepperInt3.Write(GpioPinValue.Low);
                        pinStepperInt4.Write(GpioPinValue.Low);
                        break;
                    case 3:
                        pinStepperInt1.Write(GpioPinValue.Low);
                        pinStepperInt2.Write(GpioPinValue.High);
                        pinStepperInt3.Write(GpioPinValue.High);
                        pinStepperInt4.Write(GpioPinValue.Low);
                        break;
                    case 4:
                        pinStepperInt1.Write(GpioPinValue.Low);
                        pinStepperInt2.Write(GpioPinValue.Low);
                        pinStepperInt3.Write(GpioPinValue.High);
                        pinStepperInt4.Write(GpioPinValue.Low);
                        break;
                    case 5:
                        pinStepperInt1.Write(GpioPinValue.Low);
                        pinStepperInt2.Write(GpioPinValue.Low);
                        pinStepperInt3.Write(GpioPinValue.High);
                        pinStepperInt4.Write(GpioPinValue.High);
                        break;
                    case 6:
                        pinStepperInt1.Write(GpioPinValue.Low);
                        pinStepperInt2.Write(GpioPinValue.Low);
                        pinStepperInt3.Write(GpioPinValue.Low);
                        pinStepperInt4.Write(GpioPinValue.High);
                        break;
                    case 7:
                        pinStepperInt1.Write(GpioPinValue.High);
                        pinStepperInt2.Write(GpioPinValue.Low);
                        pinStepperInt3.Write(GpioPinValue.Low);
                        pinStepperInt4.Write(GpioPinValue.High);
                        break;
                }
            }
            else if (DRIVEMODE == DriveMode.FullStep)
            {
                switch (Stroke)
                {
                    case 0:
                        pinStepperInt1.Write(GpioPinValue.High);
                        pinStepperInt2.Write(GpioPinValue.High);
                        pinStepperInt3.Write(GpioPinValue.Low);
                        pinStepperInt4.Write(GpioPinValue.Low);
                        break;
                    case 1:
                        pinStepperInt1.Write(GpioPinValue.Low);
                        pinStepperInt2.Write(GpioPinValue.High);
                        pinStepperInt3.Write(GpioPinValue.High);
                        pinStepperInt4.Write(GpioPinValue.Low);
                        break;
                    case 2:
                        pinStepperInt1.Write(GpioPinValue.Low);
                        pinStepperInt2.Write(GpioPinValue.Low);
                        pinStepperInt3.Write(GpioPinValue.High);
                        pinStepperInt4.Write(GpioPinValue.High);
                        break;
                    case 3:
                        pinStepperInt1.Write(GpioPinValue.High);
                        pinStepperInt2.Write(GpioPinValue.Low);
                        pinStepperInt3.Write(GpioPinValue.Low);
                        pinStepperInt4.Write(GpioPinValue.High);
                        break;
                }
            }
        }

        public void Dispose()
        {
            pinStepperInt1.Dispose();
            pinStepperInt2.Dispose();
            pinStepperInt3.Dispose();
            pinStepperInt4.Dispose();
            pinPhotointerrupter.Dispose();
        }
    }
}