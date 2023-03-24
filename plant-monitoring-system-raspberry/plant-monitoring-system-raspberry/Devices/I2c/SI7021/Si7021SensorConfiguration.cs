using System;

namespace plant_monitoring_system_raspberry.Devices.I2c.Si7021
{
    /// <summary>
    /// Class that represents a configuration for the temperature
    /// and humidity sensor.
    /// </summary>
    /// <remarks>
    /// Every setting has it's own property. Each has a default value
    /// that's the same as the sensor has in it's default state.
    /// </remarks>
    class Si7021SensorConfiguration
    {
        /// <value>
        /// Represents the internal heater status.
        /// </value>
        public bool IsHeating { get; set; } = false;

        /// <value>
        /// Represents the measurement resolution.
        /// </value>
        public MeasurementResolutions MeasurementResolution { get; set; } = MeasurementResolutions.T_H_14_12_BIT;

        /// <value>
        /// Represents the contition of the power supply.
        /// </value>
        public byte VddStatus { get; internal set; } = 0;

        public override string ToString()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a configuration byte from the object.
        /// </summary>
        /// <returns>
        /// The configuration byte.
        /// </returns>
        internal byte GetConfigurationByte()
        {
            byte predefinied = 0b00111010;
            if (IsHeating)
                predefinied |= 0b00000100;
            else
                predefinied &= 0b11111011;

            switch (MeasurementResolution)
            {
                case MeasurementResolutions.T_H_14_12_BIT:
                    break;
                case MeasurementResolutions.T_H_13_10_BIT:
                    predefinied |= 0b10000000;
                    break;
                case MeasurementResolutions.T_H_12_8_BIT:
                    predefinied |= 0b00000001;
                    break;
                case MeasurementResolutions.T_H_11_11_BIT:
                    predefinied |= 0b10000001;
                    break;
            }
            return predefinied;
        }
    }
}
