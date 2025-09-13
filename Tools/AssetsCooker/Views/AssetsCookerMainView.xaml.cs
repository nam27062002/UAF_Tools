using System;
using System.Globalization;
using System.Windows.Data;

namespace DANCustomTools.Tools.AssetsCooker.Views
{
    public partial class AssetsCookerMainView : System.Windows.Controls.UserControl
    {
        public AssetsCookerMainView()
        {
            InitializeComponent();
        }
    }

    public class BooleanToStatusConverter : IValueConverter
    {
        public static readonly BooleanToStatusConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCooking)
            {
                return isCooking ? "Cooking..." : "Ready";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}