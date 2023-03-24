using System;
using System.Threading.Tasks;
using Windows.Devices.I2c;

namespace plant_monitoring_system_raspberry.Devices.I2c.Arduino
{
    /// <summary>
    /// A class that is responsible for controlling the Arduino.
    /// </summary>
    class Arduino : IArduino, IDisposable
    {
        // Address of the I2C device
        private readonly byte ARDUINO_I2C_ADDR;

        // A private field that represents an I2C device
        I2cDevice arduino;

        /// <summary>
        /// Shows whether the object is initialized or not.
        /// </summary>
        public bool IsInitialized { get; private set; }

        public Arduino(byte address = 0x63)
        {
            ARDUINO_I2C_ADDR = address;
        }

        /// <summary>
        /// Constructor that sets the address of the Arduino 
        /// given in the <paramref name="address"/> parameter.
        /// </summary>
        /// <param name="address">
        /// Address of the I2D device. Default value matches with the Arduino code.
        /// </param>
        public async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("The I2C Arduino is already initialized.");
            }

            // gets the default controller for the system, can be the lightning or any provider
            I2cController controller = await I2cController.GetDefaultAsync();

            // gets the I2CDevice from the controller using the connection setting
            arduino = controller.GetDevice(new I2cConnectionSettings(ARDUINO_I2C_ADDR));

            if (arduino == null)
                throw new Exception("I2C controller not available on the system");

            IsInitialized = true;
        }

        public async Task<double> ReadThermistorAsync()
        {
            if (arduino == null)
                throw new InvalidOperationException("The I2C device must be initialized before the usage.");

            byte[] response = new byte[2];
            byte[] command = new byte[1];
            command[0] = 0x01;
            arduino.Write(command);
            await Task.Delay(5);
            arduino.Read(response);
            Array.Reverse(response);

            //if(resistance == -1) { }

            var temp = (double)BitConverter.ToInt16(response, 0) / 10000;

            return Math.Pow((
                (3.354016 * Math.Pow(10, -3)) +
                (2.569355 * Math.Pow(10, -4) * Math.Log(temp)) +
                (2.626311 * Math.Pow(10, -6) * Math.Pow(Math.Log(temp), 2)) +
                (0.675278 * Math.Pow(10, -7) * Math.Pow(Math.Log(temp), 3))
                ), -1) - 273.15;

        }

        public async Task<int> ReadWaterLevelAsync()
        {
            if (arduino == null)
                throw new InvalidOperationException("The I2C device must be initialized before the usage.");

            byte[] response = new byte[2];
            byte[] command = new byte[1];
            command[0] = 0x02;
            var status = arduino.WritePartial(command);

            if (status.Status == I2cTransferStatus.ClockStretchTimeout)
                return -2;

            await Task.Delay(20);
            status = arduino.ReadPartial(response);

            if (status.Status == I2cTransferStatus.ClockStretchTimeout)
                return -2;

            Array.Reverse(response);
            return BitConverter.ToInt16(response, 0);
        }

        /// <summary>
        /// Frees up resources.
        /// </summary>
        public void Dispose()
        {
            arduino.Dispose();
            arduino = null;
        }
    }
}
