using System;
using System.Threading.Tasks;

namespace plant_monitoring_system_raspberry.Devices.I2c.Si7021
{
    /// <summary>
    /// Functions and methods required to control the sensor.
    /// </summary>
    interface ISi7021Sensor
    {
        /// <summary>
        /// Initializes the sensor before use.
        /// </summary>
        /// <returns></returns>
        Task InitializeAsync();

        /// <summary>
        /// Executes a temperature measurement.
        /// </summary>
        /// <param name="IsHoldMasterModeActive">
        /// If true the humidity measurement is done in hold master mode. 
        /// </param>
        /// <param name="IsCrcRequired">
        /// If true the function calculates validete the reading with a CRC checksum.
        /// </param>
        /// <returns>
        /// The measured temperature.
        /// </returns>
        Task<double> MeasureTemperatureAsync(bool IsHoldMasterModeActive, bool IsCrcRequired);

        /// <summary>
        /// Executes a humidity measurement.
        /// </summary>
        /// <param name="IsHoldMasterModeActive">
        /// If true the humidity measurement is done in hold master mode. 
        /// </param>
        /// <param name="IsCrcRequired">
        /// If true the function calculates validete the reading with a CRC checksum.
        /// </param>
        /// <returns>
        /// The measured humidity.
        /// </returns>
        Task<double> MeasureHumidityAsync(bool IsHoldMasterModeActive, bool IsCrcRequired);

        /// <summary>
        /// Executes a humidity and a temperature measurement. 
        /// </summary>
        /// <param name="IsHoldMasterModeActive">
        /// If true the humidity measurement is done in hold master mode.
        /// </param>
        /// <returns>
        /// The measured temperature and humidity as a Tuple in this order.
        /// </returns>
        Task<Tuple<double, double>> MeasureTemperatureHumidityAsync(bool IsHoldMasterModeActive);

        /// <summary>
        /// Reads the configuration of the temperature and humidity sensor.
        /// </summary>
        /// <returns>
        /// The current configuration.
        /// </returns>
        Si7021SensorConfiguration ReadConfiguration();

        /// <summary>
        /// Write a new configuration to the temperature and humidity sensor
        /// witch is defined in the <paramref name="config"/> parameter.
        /// </summary>
        /// <param name="config">
        /// The configuration to be written.
        /// </param>
        void WriteConfiguration(Si7021SensorConfiguration config);

        /// <summary>
        /// Reads the EUI unique identifier from the temperature and humidity sensor.
        /// </summary>
        /// <returns>
        /// The EUI as a byte array.
        /// </returns>
        byte[] ReadEid();

        /// <summary>
        /// Reads the firmware version of the temperature and humidity sensor.
        /// </summary>
        /// <returns>
        /// The byte sent by the sensor.
        /// </returns>
        byte ReadFirmwareVersion();

        /// <summary>
        /// Reads the sensor's heating control register
        /// </summary>
        /// <returns>
        /// The intensity of the heating.
        /// </returns>
        byte ReadHeaterControlRegister();

        /// <summary>
        /// Writes the sensor's heater control register.
        /// </summary>
        /// <param name="heatingLevel">
        /// The intensity of the heating level.
        /// </param>
        void WriteHeaterControlRegister(byte heatingLevel);

        /// <summary>
        /// Resets the temperature and humidity sensor.
        /// </summary>
        void Reset();
    }
}
