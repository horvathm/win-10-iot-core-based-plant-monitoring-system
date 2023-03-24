using Microsoft.IoT.Lightning.Providers;
using System;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace plant_monitoring_system_raspberry.Devices.I2c.Ads1115
{
    /// <summary>
    /// Class that is responsible for controlling the ADS1115 ADC.
    /// </summary>
    public sealed class Ads1115Sensor : IDisposable, IAds1115Sensor
    {
        #region Fields
        // Address of the ADC
        private readonly byte ADC_I2C_ADDR;

        // Address Pointer Register addresses
        private const byte ADC_REG_POINTER_CONVERSION = 0x00;
        private const byte ADC_REG_POINTER_CONFIG = 0x01;
        private const byte ADC_REG_POINTER_LOTRESHOLD = 0x02;
        private const byte ADC_REG_POINTER_HITRESHOLD = 0x03;

        // ADC resolution constants for different conversion modes
        public const int ADC_RES = 65536;
        public const int ADC_HALF_RES = 32768;

        // A private field that represents an I2C device
        private I2cDevice adc;

        private bool fastReadAvailable = false;                 // if false you have to initialize before use read continuous
        #endregion

        /// <summary>
        /// Shows whether the object is initialized or not.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Ctor. Sets the device address given in the parameter. 
        /// </summary>
        /// <param name="ads1115Addresses">
        /// The addres thats set by connecting its address pin properly.
        /// </param>
        public Ads1115Sensor(AdcAddress ads1115Addresses = AdcAddress.GND)
        {
            ADC_I2C_ADDR = (byte)ads1115Addresses;
        }

        /// <summary>
        /// Free up the resources.
        /// </summary>
        public void Dispose()
        {
            adc.Dispose();
            adc = null;
        }

        public async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("The I2C ads1115 sensor is already initialized.");
            }

            I2cController controller;
            if (LightningProvider.IsLightningEnabled)
            {
                controller = (await I2cController.GetControllersAsync(LightningI2cProvider.GetI2cProvider()))[0];
            }
            else
            {
                controller = await I2cController.GetDefaultAsync();
            }

            var settings = new I2cConnectionSettings(ADC_I2C_ADDR)
            {
                BusSpeed = I2cBusSpeed.FastMode
            };

            adc = controller.GetDevice(settings);

            IsInitialized = true;
        }

        public void WriteConfig(byte[] config)
        {
            adc.Write(new byte[] { ADC_REG_POINTER_CONFIG, config[0], config[1] });

            fastReadAvailable = false;
        }

        public async Task<byte[]> ReadConfig()
        {
            byte[] readRegister = new byte[2];
            adc.WriteRead(new byte[] { ADC_REG_POINTER_CONFIG }, readRegister);

            await Task.Delay(10);

            var writeBuffer = new byte[] { ADC_REG_POINTER_CONVERSION };
            adc.Write(writeBuffer);

            return readRegister;
        }

        public async void TurnAlertIntoConversionReady()
        {
            byte[] bytesH = BitConverter.GetBytes(0xFFFF);
            byte[] bytesL = BitConverter.GetBytes(0x0000);

            Array.Reverse(bytesH);
            Array.Reverse(bytesL);

            var writeBufferH = new byte[] { ADC_REG_POINTER_HITRESHOLD, bytesH[0], bytesH[1] };
            var writeBufferL = new byte[] { ADC_REG_POINTER_LOTRESHOLD, bytesL[0], bytesL[1] };

            adc.Write(writeBufferH); await Task.Delay(10);
            adc.Write(writeBufferL); await Task.Delay(10);

            var writeBuffer = new byte[] { ADC_REG_POINTER_CONVERSION };
            adc.Write(writeBuffer);
        }

        public async Task WriteTreshold(int loTreshold = -32768, int highTreshold = 32767)
        {
            byte[] bytesH = BitConverter.GetBytes(highTreshold);
            byte[] bytesL = BitConverter.GetBytes(loTreshold);

            Array.Reverse(bytesH);
            Array.Reverse(bytesL);

            if (((bytesH[0] & 0x80) != 0) && ((bytesL[0] & 0x80) == 0))
                throw new ArgumentException("High treshold highest bit is 1 and low treshold highest bit is 0 witch disables treshold register");

            var writeBufferH = new byte[] { ADC_REG_POINTER_HITRESHOLD, bytesH[0], bytesH[1] };
            var writeBufferL = new byte[] { ADC_REG_POINTER_LOTRESHOLD, bytesL[0], bytesL[1] };

            adc.Write(writeBufferH); await Task.Delay(10);
            adc.Write(writeBufferL); await Task.Delay(10);

            var writeBuffer = new byte[] { ADC_REG_POINTER_CONVERSION };
            adc.Write(writeBuffer);
        }

        public async Task ReadContinuousInit(Ads1115SensorSetting setting)
        {
            if (setting.Mode != AdcMode.CONTINOUS_CONVERSION)
                throw new InvalidOperationException("You can only read in continuous mode");

            var command = new byte[] { ADC_REG_POINTER_CONFIG, ConfigA(setting), ConfigB(setting) };
            adc.Write(command);

            await Task.Delay(10);

            var writeBuffer = new byte[] { ADC_REG_POINTER_CONVERSION };
            adc.Write(writeBuffer);

            fastReadAvailable = true;
        }

        public int ReadContinuous()
        {
            if (fastReadAvailable)
            {
                var readBuffer = new byte[2];
                adc.Read(readBuffer);

                if ((byte)(readBuffer[0] & 0x80) != 0x00)
                {
                    // two's complement conversion (two's complement byte array to int16)
                    readBuffer[0] = (byte)~readBuffer[0];
                    readBuffer[1] = (byte)~readBuffer[1];
                    Array.Reverse(readBuffer);
                    return Convert.ToInt16(-1 * (BitConverter.ToInt16(readBuffer, 0) + 1));
                }
                else
                {
                    Array.Reverse(readBuffer);
                    return BitConverter.ToInt16(readBuffer, 0);
                }
            }
            else
            {
                throw new InvalidOperationException("It has to be initialized after every process that modifies configuration register");
            }
        }

        public async Task<Ads1115SensorData> ReadSingleShot(Ads1115SensorSetting setting)
        {
            if (setting.Mode != AdcMode.SINGLESHOOT_CONVERSION)
                throw new InvalidOperationException("You can only read in single shot mode");

            var sensorData = new Ads1115SensorData();
            int temp = await ReadSensorAsync(ConfigA(setting), ConfigB(setting));   //read sensor with the generated configuration bytes
            sensorData.DecimalValue = temp;

            //calculate the voltage with different resolutions in single ended and in differential mode
            if ((byte)setting.Input <= 0x03)
                sensorData.VoltageValue = DecimalToVoltage(setting.Pga, temp, ADC_RES);
            else
                sensorData.VoltageValue = DecimalToVoltage(setting.Pga, temp, ADC_HALF_RES);

            fastReadAvailable = false;

            return sensorData;
        }

        /// <summary>
        /// Executes an ADC read with the configuration given in <paramref name="configA"/>
        /// and <paramref name="configB"/> parameters and returns the result.
        /// </summary>
        /// <param name="configA">
        /// The first part of the configuration byte.
        /// </param>
        /// <param name="configB">
        /// The second part of the configuration byte.
        /// </param>
        /// <returns>
        /// The conversion result.
        /// </returns>
        private async Task<int> ReadSensorAsync(byte configA, byte configB)
        {
            var command = new byte[] { ADC_REG_POINTER_CONFIG, configA, configB };
            var readBuffer = new byte[2];
            var writeBuffer = new byte[] { ADC_REG_POINTER_CONVERSION };
            adc.Write(command);

            await Task.Delay(10);       // havent found the proper value

            adc.WriteRead(writeBuffer, readBuffer);

            if ((byte)(readBuffer[0] & 0x80) != 0x00)
            {
                // two's complement conversion (two's complement byte array to int16)
                readBuffer[0] = (byte)~readBuffer[0];
                readBuffer[1] = (byte)~readBuffer[1];
                Array.Reverse(readBuffer);
                return Convert.ToInt16(-1 * (BitConverter.ToInt16(readBuffer, 0) + 1));
            }
            else
            {
                Array.Reverse(readBuffer);
                return BitConverter.ToInt16(readBuffer, 0);
            }
        }

        /// <summary>
        /// Creates the first byte of the configuration register from the ADS1115SensorSetting 
        /// object by utilizing bit shifting.
        /// </summary>
        /// <param name="setting">
        /// Setting to be processed.
        /// </param>
        /// <returns>
        /// First part of the configuration bytes.
        /// </returns>
        private byte ConfigA(Ads1115SensorSetting setting)
        {
            byte configA = 0;
            return configA = (byte)((byte)setting.Mode << 7 | (byte)setting.Input << 4 | (byte)setting.Pga << 1 | (byte)setting.Mode);
        }

        /// <summary>
        /// Creates the second byte of the configuration register from the ADS1115SensorSetting 
        /// object by utilizing bit shifting.
        /// </summary>
        /// <param name="setting">
        /// Setting to be processed.
        /// </param>
        /// <returns>
        /// Second part of the configuration bytes.
        /// </returns>
        private byte ConfigB(Ads1115SensorSetting setting)
        {
            byte configB;
            return configB = (byte)((byte)setting.DataRate << 5 | (byte)setting.ComMode << 4 | (byte)setting.ComPolarity << 3 | (byte)setting.ComLatching << 2 | (byte)setting.ComQueue);
        }


        /// <summary>
        /// Converts the raw ADC value to voltage using the three parameters.
        /// </summary>
        /// <param name="pga">
        /// The used PGA setting.
        /// </param>
        /// <param name="temp">
        /// The ADC reading.
        /// </param>
        /// <param name="resolution">
        /// Resolution of the reading.
        /// </param>
        /// <returns>
        /// The voltage level of the reading.
        /// </returns>
        private double DecimalToVoltage(AdcPga pga, int temp, int resolution)
        {
            double voltage;

            switch (pga)
            {
                case AdcPga.G2P3:
                    voltage = 6.144;
                    break;
                case AdcPga.G1:
                    voltage = 4.096;
                    break;
                case AdcPga.G2:
                    voltage = 2.048;
                    break;
                case AdcPga.G4:
                    voltage = 1.024;
                    break;
                case AdcPga.G8:
                    voltage = 0.512;
                    break;
                case AdcPga.G16:
                default:
                    voltage = 0.256;
                    break;
            }

            return (double)temp * (voltage / (double)resolution);
        }
    }

}