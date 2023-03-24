using System;
using Windows.UI.Xaml.Data;

namespace plant_monitoring_system_raspberry.Converters
{

    /// <summary>
    /// Converter class that inverts boolean values in order to bind them
    /// to the mode selector ComboBox.
    /// </summary>
    class InvertBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Inverts the input boolean parameter. If it was true returns false and if it was
        /// false it returns true.
        /// </summary>
        /// <param name="value">bool type param to be inverted</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="language"></param>
        /// <returns>inverted value of the value parameter</returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((bool)value)
                return false;
            else
                return true;
        }
        /// <summary>
        /// Inverts the input boolean parameter. If it was true returns false and if it was
        /// false it returns true.
        /// </summary>
        /// <param name="value">bool type param to be inverted</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="language"></param>
        /// <returns>inverted value of the value parameter</returns>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if ((bool)value)
                return false;
            else
                return true;
        }
    }
}
