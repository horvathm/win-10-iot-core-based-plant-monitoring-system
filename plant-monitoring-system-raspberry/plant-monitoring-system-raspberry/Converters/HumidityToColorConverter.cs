using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace plant_monitoring_system_raspberry.Converters
{
    /// <summary>
    /// Converter class that converts humidity values to progress bar background colors.
    /// </summary>
    class HumidityToColorConverter : IValueConverter
    {
        /// <summary>
        /// Convert from humidity to color. The humidity range is divided
        /// evenly into 10 different colors. The dryest is white and the wettest
        /// is blue. The remaining colors are transitions betwen the two extremity.
        /// </summary>
        /// <param name="value">Humidity to be converted to a color</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="language"></param>
        /// <returns>SolidBrushColor type with the desired color as an object</returns>
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var scaledDownValue = Math.Round((double)value / 10);

            switch (scaledDownValue)
            {
                case 10:
                    return new SolidColorBrush(Color.FromArgb(255, 0, 102, 255));
                case 9:
                    return new SolidColorBrush(Color.FromArgb(255, 26, 117, 255));
                case 8:
                    return new SolidColorBrush(Color.FromArgb(255, 51, 133, 255));
                case 7:
                    return new SolidColorBrush(Color.FromArgb(255, 77, 148, 255));
                case 5:
                    return new SolidColorBrush(Color.FromArgb(255, 102, 163, 255));
                case 4:
                    return new SolidColorBrush(Color.FromArgb(255, 128, 179, 255));
                case 3:
                    return new SolidColorBrush(Color.FromArgb(255, 153, 194, 255));
                case 2:
                    return new SolidColorBrush(Color.FromArgb(255, 179, 209, 255));
                case 1:
                    return new SolidColorBrush(Color.FromArgb(255, 204, 224, 255));
                case 0:
                default:
                    return new SolidColorBrush(Color.FromArgb(255, 230, 240, 255));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

