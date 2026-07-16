using ClrWinApi;

using Explorer.Controls;

using Microsoft.UI.Xaml.Media.Imaging;

using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace FileCommander;

static class ShellIconCache
{
    public static BitmapSource? GetIcon(string? index)
    {
        if (string.IsNullOrWhiteSpace(index))
            return null;
        else if (cache.TryGetValue(index, out var val))
            return val.Value.Result;
        else
            return null;
    }

    public static async Task<string?> GetAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        await cache.GetOrAdd(path, k => new Lazy<Task<BitmapSource?>>(() => LoadIconCoreAsync(k))).Value;
        return path;
    }

    static async Task<BitmapSource?> LoadIconCoreAsync(string key)
    {
        var hIcon = ShellIconInterop.GetIconHandle(key, 16);
        var res = await ToBitmapImageAsync(hIcon);
        Api.DestroyIcon(hIcon);
        return res;
    }

    static async Task<BitmapImage?> ToBitmapImageAsync(nint hIcon)
    {
        using var icon = Icon.FromHandle(hIcon);
        using var bitmap = icon.ToBitmap();
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;
        var image = new BitmapImage();
        await image.SetSourceAsync(stream.AsRandomAccessStream());
        return image;
    }

    static readonly ConcurrentDictionary<string, Lazy<Task<BitmapSource?>>> cache = [];
}
