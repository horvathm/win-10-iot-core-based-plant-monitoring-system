using System;
using System.Threading.Tasks;

namespace plant_monitoring_system_raspberry.Devices.I2c.Ads1115
{
    /// <summary>
    /// Functions and methods required to control the ADC.
    /// </summary>
    interface IAds1115Sensor
    {
        /// <summary>
        /// Writes the configuration given in the <paramref name="config"/> parameter
        /// to the configuration register of the ADC.
        /// </summary>
        /// <param name="config">
        /// Two element byte array with the configuration in it.
        /// </param>
        void WriteConfig(byte[] config);

        /// <summary>
        /// Reads the configuration register.
        /// </summary>
        /// <returns>
        /// Two element byte array with the configuration of the ADC in it.
        /// </returns>
        Task<byte[]> ReadConfig();

        /// <summary>
        /// Turns the ALERT pin of the ADC to conversion ready pin.
        /// </summary>
        void TurnAlertIntoConversionReady();

        /// <summary>
        /// Writes the treshold registers to the given values.
        /// </summary>
        /// <param name="loTreshold">
        /// Value of the low treshold register.
        /// </param>
        /// <param name="highTreshold">
        /// Value of the high treshold register.
        /// </param>
        /// <returns></returns>
        Task WriteTreshold(int loTreshold, int highTreshold);

        /// <summary>
        /// Initializes the read continuous mode.
        /// </summary>
        /// <remarks>
        /// <param name="setting">
        /// The desired setting to be set. 
        /// </param>
        /// <returns></returns>
        Task ReadContinuousInit(Ads1115SensorSetting setting);

        /// <summary>
        /// Executes a measurement in continuous conversion mode.
        /// </summary>
        /// <returns>
        /// Returns the raw ADC reading.
        /// </returns>
        int ReadContinuous();

        /// <summary>
        /// Executes a measurement in single shoot mode.
        /// </summary>
        /// <param name="setting">
        /// The desired setting to be set. 
        /// </param>
        /// <returns>
        /// An object that represents the sensor setting. 
        /// </returns>
        Task<Ads1115SensorData> ReadSingleShot(Ads1115SensorSetting setting);
    }
}
