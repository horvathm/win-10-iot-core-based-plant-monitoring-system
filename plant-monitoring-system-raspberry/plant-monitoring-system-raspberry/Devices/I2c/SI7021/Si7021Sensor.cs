using Microsoft.IoT.Lightning.Providers;
using System;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace plant_monitoring_system_raspberry.Devices.I2c.Si7021
{
    /// <summary>
    /// Measurement options.
    /// </summary>
    public enum MeasurementResolutions : byte
    {
        T_H_14_12_BIT = 0,
        T_H_13_10_BIT = 2,
        T_H_12_8_BIT = 1,
        T_H_11_11_BIT = 3
    }

    /// <summary>
    /// A class that is responsible for controlling the temperature
    /// and humidity sensor.
    /// </summary>
    class Si7021Sensor : ISi7021Sensor, IDisposable
    {
        #region Fields
        // Address of the sensor
        private const byte DEVICE_ADDRESS = 0x40;

        // I2C Commands
        private const byte _MEAS_REL_HUM_HOLD = 0xE5;
        private const byte _MEAS_REL_HUM_NO_HOLD = 0xF5;
        private const byte _MEAS_TEMP_HOLD = 0xE3;
        private const byte _MEAS_TEMP_NO_HOLD = 0xF3;
        private const byte _MEAS_PREV_TEMP = 0xE0;
        private const byte _RESET = 0xFE;
        private const byte _USER_REG_WRITE = 0xE6;
        private const byte _USER_REG_READ = 0xE7;
        private const byte _HEATER_CTLR_REG_WRITE = 0x51;
        private const byte _HEATER_CTLR_REG_READ = 0x11;
        private const byte _READ_EID_A_1 = 0xFA;
        private const byte _READ_EID_A_2 = 0x0F;
        private const byte _READ_EID_B_1 = 0xFC;
        private const byte _READ_EID_B_2 = 0xC9;
        private const byte _READ_FW_VER_0 = 0x84;
        private const byte _READ_FW_VER_1 = 0XB8;

        // A private field that represents an I2C device
        private I2cDevice sensorSI7021;
        #endregion

        /// <summary>
        /// Shows whether the object is initialized or not.
        /// </summary>
        public bool IsInitialized { private set; get; }

        /// <summary>
        /// Frees up the resources.
        /// </summary>
        public void Dispose()
        {
            sensorSI7021.Dispose();
        }

        public async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("The SI7021 temperature and humidity sensor is already initialized.");
            }

            // gets the default controller for the system, can be the lightning or any provider
            I2cController controller;
            if (LightningProvider.IsLightningEnabled)
            {
                controller = (await I2cController.GetControllersAsync(LightningI2cProvider.GetI2cProvider()))[0];
            }
            else
            {
                controller = await I2cController.GetDefaultAsync();
            }

            var settings = new I2cConnectionSettings(DEVICE_ADDRESS)
            {
                BusSpeed = I2cBusSpeed.StandardMode
            };

            // gets the I2CDevice from the controller using the connection setting
            sensorSI7021 = controller.GetDevice(settings);

            IsInitialized = true;
        }

        public void Reset()
        {
            sensorSI7021.Write(new byte[] { _RESET });
        }

        public async Task<double> MeasureTemperatureAsync(bool IsHoldMasterModeActive, bool IsCrcRequired)
        {
            int raw_t = 0;
            if (IsHoldMasterModeActive)
            {
                if (IsCrcRequired)
                {
                    var reading = WriteRead(_MEAS_TEMP_HOLD, 3);
                    if (!VerifyCRC(new byte[] { reading[0], reading[1] }, reading[2]))
                        throw new Exception("CRC verification failed. Data has damaged.");
                    reading[1] &= 0b11111100;
                    var temp = new byte[] { reading[0], reading[1] };
                    Array.Reverse(temp);
                    raw_t = BitConverter.ToInt16(temp, 0);

                }
                else
                {
                    var reading = WriteRead(_MEAS_TEMP_HOLD, 2);
                    reading[1] &= 0b11111100;
                    Array.Reverse(reading);
                    raw_t = BitConverter.ToInt16(reading, 0);
                }
            }
            else
            {
                if (IsCrcRequired)
                {
                    var reading = await WriteReadAsync(_MEAS_TEMP_NO_HOLD, 3, 13);  // 3rd param is max required time plus some offset
                    if (!VerifyCRC(new byte[] { reading[0], reading[1] }, reading[2]))
                        throw new Exception("CRC verification failed. Data has damaged.");
                    reading[1] &= 0b11111100;
                    var temp = new byte[] { reading[0], reading[1] };
                    Array.Reverse(temp);
                    raw_t = BitConverter.ToInt16(temp, 0);
                }
                else
                {
                    var reading = await WriteReadAsync(_MEAS_TEMP_NO_HOLD, 2, 13);  // 3rd param is max required time plus some offset
                    reading[1] &= 0b11111100;
                    Array.Reverse(reading);
                    raw_t = BitConverter.ToInt16(reading, 0);
                }
            }
            return ((double)(175.72 * raw_t) / 65536) - 46.85;
        }

        public async Task<double> MeasureHumidityAsync(bool IsHoldMasterModeActive, bool IsCrcRequired)
        {
            int raw_rh = 0;
            if (IsHoldMasterModeActive)
            {
                if (IsCrcRequired)
                {
                    var reading = WriteRead(_MEAS_REL_HUM_HOLD, 3);
                    if (!VerifyCRC(new byte[] { reading[0], reading[1] }, reading[2]))
                        throw new Exception("CRC verification failed. Data has damaged.");
                    reading[1] &= 0b11111100;
                    var temp = new byte[] { reading[0], reading[1] };
                    Array.Reverse(temp);
                    raw_rh = BitConverter.ToInt16(temp, 0);
                }
                else
                {
                    var reading = WriteRead(_MEAS_REL_HUM_HOLD, 2);
                    reading[1] &= 0b11111100;
                    Array.Reverse(reading);
                    raw_rh = BitConverter.ToInt16(reading, 0);
                }
            }
            else
            {
                if (IsCrcRequired)
                {
                    var reading = await WriteReadAsync(_MEAS_REL_HUM_NO_HOLD, 3, 16);   // 3rd param is max required time plus some offset
                    if (!VerifyCRC(new byte[] { reading[0], reading[1] }, reading[2]))
                        throw new Exception("CRC verification failed. Data has damaged.");
                    reading[1] &= 0b11111100;
                    var temp = new byte[] { reading[0], reading[1] };
                    Array.Reverse(temp);
                    raw_rh = BitConverter.ToInt16(temp, 0);
                }
                else
                {
                    var reading = await WriteReadAsync(_MEAS_REL_HUM_NO_HOLD, 2, 16);   // 3rd param is max required time plus some offset
                    reading[1] &= 0b11111100;
                    Array.Reverse(reading);
                    raw_rh = BitConverter.ToInt16(reading, 0);
                }
            }
            return ((double)(125 * raw_rh) / 65536) - 6;
        }

        public async Task<Tuple<double, double>> MeasureTemperatureHumidityAsync(bool IsHoldMasterModeActive)
        {
            var humidity = await MeasureHumidityAsync(IsHoldMasterModeActive, false);
            var temperatureBytes = WriteRead(_MEAS_PREV_TEMP, 2);
            temperatureBytes[1] &= 0b11111100;
            Array.Reverse(temperatureBytes);
            int raw_t = BitConverter.ToInt16(temperatureBytes, 0);
            var temperature = ((175.72 * raw_t) / 65536) - 46.85;
            return new Tuple<double, double>(temperature, humidity);
        }

        public Si7021SensorConfiguration ReadConfiguration()
        {
            byte[] outputBuffer = new byte[1];
            // Reading the configuration register
            sensorSI7021.WriteRead(new byte[] { _USER_REG_READ }, outputBuffer);
            var temp = new Si7021SensorConfiguration();

            // Setting the objects that represents the sensor setting by processing the reading
            if ((outputBuffer[0] & 0b01000000) != 0)
                temp.VddStatus = 1;

            if ((outputBuffer[0] & 0b00000100) != 0)
                temp.IsHeating = true;

            var measurementResolution = (outputBuffer[0] >> 6) | (outputBuffer[0] & 0b00000001);
            switch (measurementResolution)
            {
                case 3:
                    temp.MeasurementResolution = MeasurementResolutions.T_H_11_11_BIT;
                    break;
                case 2:
                    temp.MeasurementResolution = MeasurementResolutions.T_H_13_10_BIT;
                    break;
                case 1:
                    temp.MeasurementResolution = MeasurementResolutions.T_H_12_8_BIT;
                    break;
                case 0:
                    temp.MeasurementResolution = MeasurementResolutions.T_H_14_12_BIT;
                    break;
            }

            return temp;
        }

        public void WriteConfiguration(Si7021SensorConfiguration config)
        {
            sensorSI7021.Write(new byte[] { _USER_REG_WRITE, config.GetConfigurationByte() });
        }

        // CRC check omitted
        public byte[] ReadEid()
        {
            byte[] result = new byte[8];
            byte[] reading = new byte[8];
            sensorSI7021.WriteRead(new byte[] { _READ_EID_A_1, _READ_EID_A_2 }, reading);

            result[7] = reading[7];
            result[6] = reading[5];
            result[5] = reading[3];
            result[4] = reading[1];

            sensorSI7021.WriteRead(new byte[] { _READ_EID_B_1, _READ_EID_B_2 }, reading);

            result[3] = reading[7];
            result[2] = reading[5];
            result[1] = reading[3];
            result[0] = reading[1];

            return result;
        }

        public byte ReadFirmwareVersion()
        {
            byte[] outputBuffer = new byte[1];
            sensorSI7021.WriteRead(new byte[] { _READ_FW_VER_0, _READ_FW_VER_1 }, outputBuffer);
            return outputBuffer[0];
        }

        public byte ReadHeaterControlRegister()
        {
            byte[] outputBuffer = new byte[1];
            // Writes the read heater control register command and then reads to the output buffer
            sensorSI7021.WriteRead(new byte[] { _HEATER_CTLR_REG_READ }, outputBuffer);
            return outputBuffer[0];
        }

        public void WriteHeaterControlRegister(byte heatingLevel)
        {
            if (heatingLevel <= 0 || heatingLevel >= 16)
                throw new InvalidOperationException("Heating level must be at least 0 and less than 15");

            // Writes the heating level to the heater control register
            sensorSI7021.Write(new byte[] { _HEATER_CTLR_REG_WRITE, heatingLevel });
            // heating level should get the previous value with OR relation or with full 1 digits where reserved
        }

        #region PrivateHelperMethods
        /// <summary>
        /// Do the CRC verification of the senso reading. 
        /// </summary>
        /// <remarks>
        /// This function calculates the CRC checksum from the sensor 
        /// reading and compares the sensor received CRC. The function is based on 
        /// the following forum thread: https://www.silabs.com/community/sensors/forum.topic.html/how_to_calculatecrc-sCTY
        /// </remarks>
        /// <param name="receivedData">
        /// The sensor reading.
        /// /param>
        /// <param name="receivedCRC">
        /// The received CRC after the sensor reading.
        /// </param>
        /// <returns>
        /// Returns true if the reading was not corrupted.
        /// </returns>
        private bool VerifyCRC(byte[] receivedData, byte receivedCRC)
        {
            byte calculatedCRC = 0x00;
            byte i, j;
            for (i = 0; i < 2; i++)
            {
                calculatedCRC ^= receivedData[i];
                for (j = 8; j > 0; j--)
                {
                    if ((calculatedCRC & 0x80) != 0)
                        calculatedCRC = (byte)((byte)(calculatedCRC << 1) ^ 0x131);
                    else
                        calculatedCRC = (byte)(calculatedCRC << 1);
                }
            }

            // If the calculated CRC and the received CRC then the data is corrupted
            if (calculatedCRC != receivedCRC)
                return false;

            // Success
            return true;
        }

        /// <summary>
        /// Executes a WriteRead function call on the temperature and humidity sensor.
        /// </summary>
        /// <param name="command">
        /// The command to be sent to the sensor.
        /// </param>
        /// <param name="length"> 
        /// Number of bytes to be read.
        /// </param>
        /// <returns>
        /// The byte array sent by the sensor.
        /// </returns>
        private byte[] WriteRead(byte command, int length)
        {
            var readBuffer = new byte[length];
            sensorSI7021.WriteRead(new byte[] { command }, readBuffer);
            return readBuffer;
        }

        /// <summary>
        /// Executes a Write function call and <paramref name="delay"/> milliseconds 
        /// later a Read funciton call on the temperature and humidity sensor.
        /// </summary>
        /// <param name="command">
        /// Command to be sent to the sensor.
        /// </param>
        /// <param name="length"> 
        /// Number of bytes to be read.
        /// </param>
        /// <param name="delay">
        /// The delay between the Write and Read function call.
        /// </param>
        /// <returns>
        /// The byte array sent by the sensor.
        /// </returns>
        private async Task<byte[]> WriteReadAsync(byte command, int length, int delay)
        {
            var readBuffer = new byte[length];
            sensorSI7021.Write(new byte[] { command });
            await Task.Delay(delay);
            sensorSI7021.Read(readBuffer);
            return readBuffer;
        }


        /// <summary>
        /// Function helps to determine the waiting time in no hold master mode.
        /// </summary>
        /// <remarks>
        /// Depending on witch measurement resolution is used during the temperature
        /// measurement the function returns the maximum waiting times in milliseconds.
        /// </remarks>
        /// <param name="resolution">
        /// Enumeration that represents the measurement resolution in the configuration register.
        /// </param>
        /// <returns>
        /// Maximal time required for the measurement.
        /// </returns>
        private double TemperatureMeasurementDelay(MeasurementResolutions resolution)
        {
            switch (resolution)
            {
                case MeasurementResolutions.T_H_14_12_BIT:
                    return 10.8;
                case MeasurementResolutions.T_H_12_8_BIT:
                    return 3.8;
                case MeasurementResolutions.T_H_13_10_BIT:
                    return 6.2;
                case MeasurementResolutions.T_H_11_11_BIT:
                    return 2.4;
                default:
                    return 10.8;
            }
        }

        /// <summary>
        /// Function helps to determine the waiting time in no hold master mode.
        /// </summary>
        /// <remarks>
        /// Depending on witch measurement resolution is used during the humidity
        /// measurement the function returns the maximum waiting times in milliseconds.
        /// </remarks>
        /// <param name="resolution">
        /// Enumeration that represents the measurement resolution in the configuration register.
        /// </param>
        /// <returns>
        /// Maximal time required for the measurement.
        /// </returns>
        private double HumidityMeasurementDelay(MeasurementResolutions resolution)
        {
            switch (resolution)
            {
                case MeasurementResolutions.T_H_14_12_BIT:
                    return 12;
                case MeasurementResolutions.T_H_12_8_BIT:
                    return 3.8;
                case MeasurementResolutions.T_H_13_10_BIT:
                    return 4.5;
                case MeasurementResolutions.T_H_11_11_BIT:
                    return 7;
                default:
                    return 12;
            }
        }
        #endregion
    }
}


