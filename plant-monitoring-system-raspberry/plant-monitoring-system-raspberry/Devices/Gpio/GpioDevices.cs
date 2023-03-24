using Microsoft.IoT.Lightning.Providers;
using System;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace plant_monitoring_system_raspberry.Devices.Gpio
{
    /// <summary>
    /// A class that is responsible for controlling some GPIO pins.
    /// </summary>
    class GpioDevices : IGpioDevices, IDisposable
    {
        // GPIO pin numbers
        private const int RELAY_PIN = 26;
        private const int SOIL_POWER_SWITCH_PIN = 21;

        // A private fields representing GPIO pins
        GpioPin pinSensorPowerSwitch;
        GpioPin pinRelaySwitch;

        /// <summary>
        /// Shows whether the object is initialized or not.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Frees up resources.
        /// </summary>
        public void Dispose()
        {
            pinSensorPowerSwitch.Dispose();
            pinRelaySwitch.Dispose();
        }

        public async Task InitializeAsync()
        {
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

                // Opening the soil moisture power supply switch pin as an output pin
                pinSensorPowerSwitch = gpioController.OpenPin(SOIL_POWER_SWITCH_PIN);
                pinSensorPowerSwitch.SetDriveMode(GpioPinDriveMode.Output);
                pinSensorPowerSwitch.Write(GpioPinValue.Low);

                // Opening the relay turn on-off pin as an output pin
                pinRelaySwitch = gpioController.OpenPin(RELAY_PIN);
                pinRelaySwitch.SetDriveMode(GpioPinDriveMode.Output);
                pinRelaySwitch.Write(GpioPinValue.High);

                IsInitialized = true;
            }
        }

        public void SoilSensorPowerOff()
        {
            pinSensorPowerSwitch.Write(GpioPinValue.Low);
        }

        public void SoilSensorPowerOn()
        {
            pinSensorPowerSwitch.Write(GpioPinValue.High);
        }

        public async Task StartIrrigationAsync(int interval)
        {
            TurnRelayOn();
            await Task.Delay(interval);
            TurnRelayOff();
        }

        public void TurnRelayOff()
        {
            pinRelaySwitch.Write(GpioPinValue.High);
        }

        public void TurnRelayOn()
        {
            pinRelaySwitch.Write(GpioPinValue.Low);
        }
    }
}