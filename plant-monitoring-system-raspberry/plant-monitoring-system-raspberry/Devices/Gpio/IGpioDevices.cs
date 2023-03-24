using System.Threading.Tasks;

namespace plant_monitoring_system_raspberry.Devices.Gpio
{
    /// <summary>
    /// Functions and methods required to control the sensor.
    /// </summary>
    interface IGpioDevices
    {
        /// <summary>
        /// Initializes the GPIO pins before use.
        /// </summary>
        /// <returns></returns>
        Task InitializeAsync();

        /// <summary>
        /// The function does a <paramref name="interval"/> millisecond 
        /// length irrigation process.
        /// </summary>
        /// <param name="interval">
        /// The lenth of the irrigation in milliseconds.
        /// </param>
        /// <returns></returns>
        Task StartIrrigationAsync(int interval);

        /// <summary>
        /// Turns the relay on.
        /// </summary>
        void TurnRelayOn();

        /// <summary>
        /// Turns the relay off.
        /// </summary>
        void TurnRelayOff();

        /// <summary>
        /// Turns the powering for the soil moisture sensors on.
        /// </summary>
        void SoilSensorPowerOn();

        /// <summary>
        /// Turns the powering for the soil moisture sensors off.
        /// </summary>
        void SoilSensorPowerOff();

    }
}