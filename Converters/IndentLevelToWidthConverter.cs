using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DANCustomTools.Converters
{
	public class IndentLevelToWidthConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int level = 0;
			if (value is int i) level = i;
			int perLevel = 12;
			return new Thickness(level * perLevel, 0, 0, 0).Left;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}



