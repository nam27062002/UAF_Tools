#nullable enable
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DANCustomTools.Converters
{
    public static class BitmapToImageSourceConverter
    {
        public static ImageSource? ConvertBitmapToImageSource(Bitmap? bitmap)
        {
            if (bitmap == null)
                return null;

            try
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
#pragma warning disable CA1416 // Validate platform compatibility
                        bitmap.Save(memory, ImageFormat.Png);
#pragma warning restore CA1416
                    }
                    else
                    {
                        throw new PlatformNotSupportedException("Image conversion is only supported on Windows");
                    }
                    memory.Position = 0;

                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to convert bitmap: {ex.Message}");
                return null;
            }
        }
    }
}
