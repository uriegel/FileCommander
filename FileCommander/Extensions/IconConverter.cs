using Microsoft.UI.Xaml.Media.Imaging;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

using Windows.Storage.Streams;

namespace Explorer.Controls;

internal static class IconConverter
{
    public static async Task<BitmapImage?> ToBitmapImageAsync(IntPtr hIcon)
    {
        using var icon = Icon.FromHandle(hIcon);

        using var bitmap = icon.ToBitmap();

        using var stream = new MemoryStream();

        bitmap.Save(stream, ImageFormat.Png);

        stream.Position = 0;

        var image = new BitmapImage();

        await image.SetSourceAsync(
            stream.AsRandomAccessStream());

        return image;
    }
}