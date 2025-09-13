#nullable enable
using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.Versioning;
using System.Windows.Data;
using DANCustomTools.Models.SceneExplorer;
using DANCustomTools.ViewModels;
using Brushes = System.Drawing.Brushes;

namespace DANCustomTools.Converters
{
    public class ObjectTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                ActorModel => "Actor",
                FriseModel => "Frise",
                _ => "Unknown"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToOnlineStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isOnline)
            {
                return isOnline ? "Online" : "Offline";
            }
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [SupportedOSPlatform("windows")]
    public class BoolToStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isOnline)
            {
                return isOnline ? Brushes.Green : Brushes.Gray;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ConnectionStatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status == "Connected" ? System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;
            }
            return System.Windows.Media.Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TreeItemTextFallbackConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SceneTreeItemType itemType)
            {
                return itemType switch
                {
                    SceneTreeItemType.Scene => "🌐",
                    SceneTreeItemType.Actor => "🎭",
                    SceneTreeItemType.Frise => "🖼️",
                    SceneTreeItemType.ActorSet => "📦",
                    SceneTreeItemType.FriseSet => "📁",
                    _ => "📄"
                };
            }
            return "📄";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TreeItemIconConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SceneTreeItemType itemType)
            {
                Bitmap? bitmap = itemType switch
                {
                    SceneTreeItemType.Scene => Properties.Resources.SceneBitmap,
                    SceneTreeItemType.Actor => Properties.Resources.ActorIcon,
                    SceneTreeItemType.Frise => Properties.Resources.FriseIcon,
                    SceneTreeItemType.ActorSet => Properties.Resources.OpenScenes,
                    SceneTreeItemType.FriseSet => Properties.Resources.OpenScenes,
                    _ => Properties.Resources.ScenePropertyIcon
                };

                return BitmapToImageSourceConverter.ConvertBitmapToImageSource(bitmap);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
