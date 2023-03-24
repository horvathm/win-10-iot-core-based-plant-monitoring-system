using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace plant_monitoring_system_raspberry.Devices.I2c.Ads1115
{
    #region Enumerations
    // Possible ads1115 addresses an be set via connecting ADDRESS pin to:  0x48:GND, 0x49:VCC, 0x4A:SDA, 0x4B:SCL
    public enum AdcAddress : byte { GND = 0x48, VCC = 0x49, SDA = 0x4A, SCL = 0x4B }
    public enum AdcInput : byte { A0_SE = 0x04, A1_SE = 0x05, A2_SE = 0x06, A3_SE = 0x07, A01_DIFF = 0x00, A03_DIFF = 0x01, A13_DIFF = 0x02, A23_DIFF = 0x03 }
    public enum AdcPga : byte { G2P3 = 0x00, G1 = 0x01, G2 = 0x02, G4 = 0x03, G8 = 0x04, G16 = 0x05 }
    public enum AdcMode : byte { CONTINOUS_CONVERSION = 0x00, SINGLESHOOT_CONVERSION = 0x01 }
    public enum AdcDataRate : byte { SPS8 = 0X00, SPS16 = 0X01, SPS32 = 0X02, SPS64 = 0X03, SPS128 = 0X04, SPS250 = 0X05, SPS475 = 0X06, SPS860 = 0X07 }
    public enum AdcComparatorMode : byte { TRADITIONAL = 0x00, WINDOW = 0x01 }
    public enum AdcComparatorPolarity : byte { ACTIVE_LOW = 0x00, ACTIVE_HIGH = 0x01 }
    public enum AdcComparatorLatching : byte { LATCHING = 0x00, NONLATCHING = 0x01 }
    public enum AdcComparatorQueue : byte { ASSERT_AFTER_ONE = 0x01, ASSERT_AFTER_TWO = 0x02, ASSERT_AFTER_FOUR = 0x04, DISABLE_COMPARATOR = 0x03 }
    #endregion

    /// <summary>
    /// Class that contains the settings of the ADC. The default values are from the documentation.
    /// </summary>
    public class Ads1115SensorSetting : INotifyPropertyChanged
    {
        #region Properties
        /// <value>
        /// The input to be measured.
        /// </value>
        public AdcInput Input
        {
            get { return _input; }
            set { Set(ref _input, value); }
        }
        private AdcInput _input = AdcInput.A1_SE;

        /// <value>
        /// The setting of the programmable gain amplifier.
        /// </value>
        public AdcPga Pga
        {
            get { return _pga; }
            set { Set(ref _pga, value); }
        }
        private AdcPga _pga = AdcPga.G2;

        /// <value>
        /// The operating mode of the ADC.
        /// </value>
        public AdcMode Mode
        {
            get { return _mode; }
            set { Set(ref _mode, value); }
        }
        private AdcMode _mode = AdcMode.SINGLESHOOT_CONVERSION;

        /// <value>
        /// The datarate that's used for conversion.
        /// </value>
        public AdcDataRate DataRate
        {
            get { return _dataRate; }
            set { Set(ref _dataRate, value); }
        }
        private AdcDataRate _dataRate = AdcDataRate.SPS128;

        /// <value>
        /// Setting of the comparator mode.
        /// </value>
        public AdcComparatorMode ComMode
        {
            get { return _comMode; }
            set { Set(ref _comMode, value); }
        }
        private AdcComparatorMode _comMode = AdcComparatorMode.TRADITIONAL;

        /// <value>
        /// Setting of the comparator polarity.
        /// </value>
        public AdcComparatorPolarity ComPolarity
        {
            get { return _comPolarity; }
            set { Set(ref _comPolarity, value); }
        }
        private AdcComparatorPolarity _comPolarity = AdcComparatorPolarity.ACTIVE_LOW;

        /// <value>
        /// Set whether the comparator latches after it was set or not.
        /// </value>
        public AdcComparatorLatching ComLatching
        {
            get { return _comLatching; }
            set { Set(ref _comLatching, value); }
        }
        private AdcComparatorLatching _comLatching = AdcComparatorLatching.LATCHING;

        /// <value>
        /// Setting of how many samlpes has to be read
        /// before the ALERT pin latches.
        /// </value>
        public AdcComparatorQueue ComQueue
        {
            get { return _comQueue; }
            set { Set(ref _comQueue, value); }
        }
        private AdcComparatorQueue _comQueue = AdcComparatorQueue.DISABLE_COMPARATOR;
        #endregion

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        public bool Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            // if unchanged return false
            if (Equals(storage, value))
                return false;
            storage = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        private void RaisePropertyChanged(string propertyName)
        {
            // if PropertyChanged not null call the Invoke method
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

    }
}