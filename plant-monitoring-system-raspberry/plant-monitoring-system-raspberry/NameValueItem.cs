using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace plant_monitoring_system_raspberry
{
    /// <summary>
    /// Class that contains the timestamp and the measured resistance value for the luminosity
    /// </summary>
    class NameValueItem : INotifyPropertyChanged
    {
        /// <value>
        /// The time when the measurement was taken.
        /// </value>
        public DateTime Date { get; set; }

        /// <value>
        /// Raw luminosity data from the ADC.
        /// </value>
        public int Value
        {
            get { return _value; }
            set { Set(ref _value, value); }
        }
        private int _value;

        /// <summary>
        /// Constructor that uploads <paramref name="Date" /> 
        /// with the current time.
        /// </summary>
        public NameValueItem()
        {
            Date = DateTime.Now;
        }

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