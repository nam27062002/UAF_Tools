using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DANCustomTools.Converters
{
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int count = 0;
            if (value is int i)
                count = i;
            // Visible when count == 0 (no data), otherwise Collapsed
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}

