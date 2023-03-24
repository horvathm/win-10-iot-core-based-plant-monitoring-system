using System.Threading.Tasks;

namespace plant_monitoring_system_raspberry.Devices.I2c.Arduino
{
    /// <summary>
    /// Functions and methods required to control the sensor.
    /// </summary>
    interface IArduino
    {
        /// <summary>
        /// Initializes the sensor before use.
        /// </summary>
        /// <returns></returns>
        Task InitializeAsync();

        /// <summary>
        /// Reads water level from the Arduino.
        /// </summary>
        /// <returns>
        /// Water level in centimeters rounded to decimal numbers.
        /// </returns>
        Task<int> ReadWaterLevelAsync();

        /// <summary>
        /// Reads the temperature from the Arduino.
        /// </summary>
        /// <returns>
        /// The temperature.
        /// </returns>
        Task<double> ReadThermistorAsync();
    }
}
