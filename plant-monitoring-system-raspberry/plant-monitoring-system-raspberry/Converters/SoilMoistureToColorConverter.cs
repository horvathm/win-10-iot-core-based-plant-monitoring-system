using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace plant_monitoring_system_raspberry.Converters
{
    /// <summary>
    /// Converter class that classify the soil moisture values from 1 to 5 where 5 is
    /// the wettest soil and 1 is the dryest category and assigns a color to them in order
    /// to be binded to ProgressBar's Foreground property
    /// </summary>
    /// <remarks>
    /// The category values reflects the used soil moisture sensor's characteristics trying
    /// to linearize its non-linear characteristics.
    /// </remarks>
    class SoilMoistureToColorConverter : IValueConverter
    {
        /// <summary>
        /// Determines the category wether the soil is dry or not. The categories are from
        /// 1 to 5 where 1 is the dryest.
        /// </summary>
        /// <param name="value">The raw ADC value measured from the soil moisture sensor</param>
        /// <returns>Number of the determined category</returns>
        public static int ConvertValueToCategory(int value)
        {
            if (value > 27500)
                return 5;
            if (value > 17500)
                return 4;
            if (value > 12000)
                return 3;
            if (value > 9000)
                return 2;
            else
                return 1;
        }

        /// <summary>
        /// Converts value to color according to the predefinied categories.
        /// </summary>
        /// <param name="value">The raw ADC value read from the soil moisture sensor</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="language"></param>
        /// <returns>SolidBrushColor as on object with the appropirate color</returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            switch (ConvertValueToCategory((int)value))
            {
                case 5:
                    return new SolidColorBrush(Colors.Red);
                case 4:
                    return new SolidColorBrush(Colors.Orange);
                case 3:
                    return new SolidColorBrush(Colors.Yellow);
                case 2:
                    return new SolidColorBrush(Colors.GreenYellow);
                case 1:
                default:
                    return new SolidColorBrush(Colors.LawnGreen);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
