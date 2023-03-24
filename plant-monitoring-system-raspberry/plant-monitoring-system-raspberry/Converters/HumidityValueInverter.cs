using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace plant_monitoring_system_raspberry.Converters
{
    /// <summary>
    /// Converter class that inverts soil humidity values in order to be displayed 
    /// on the progress bar.
    /// </summary>
    /// <remarks>
    /// Humidity values are decreasing. Dry ground gives bigger values than wet. 
    /// This class inverts them in order to display increasing values on the progress 
    /// bar while the values on the textblocks displays the original data.
    /// </remarks>
    class HumidityValueInverter : IValueConverter
    {
        private const int LO_TRESHOLD = 0;
        private const int HI_TRESHOLD = 32768;

        /// <summary>
        /// Invert soil moisture values by subtract measured from the highest possible value.
        /// </summary>
        /// <param name="value">Soil humidity value read by the ADC</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="language"></param>
        /// <returns>Inverted value of the soil humidity</returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((int)value > HI_TRESHOLD)
                return 0;
            if ((int)value < LO_TRESHOLD)
                return HI_TRESHOLD;

            var temp = HI_TRESHOLD - (int)value;

            return temp;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
