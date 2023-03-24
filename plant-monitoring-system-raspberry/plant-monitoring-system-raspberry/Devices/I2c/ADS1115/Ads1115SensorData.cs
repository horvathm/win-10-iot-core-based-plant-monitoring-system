namespace plant_monitoring_system_raspberry.Devices.I2c.Ads1115
{
    /// <summary>
    /// A struct that stores the ADC reading in different formats. 
    /// It contains a decimal value and a calculated voltage value.
    /// </summary>
    public struct Ads1115SensorData
    {
        public int DecimalValue { get; set; }
        public double VoltageValue { get; set; }
    }
}