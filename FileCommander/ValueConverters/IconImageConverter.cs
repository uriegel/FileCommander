using ClrWinApi;

using CsTools.Extensions;

using FileCommander.Controllers;
using FileCommander.Icon;

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

using System;
using System.Collections.Concurrent;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace FileCommander.ValueConverters;

public class IconImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
        => (value is ConflictItem conflict)
            ? ShellIconCache.GetIcon(conflict.IconIndex)
            : null;
  
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}

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
        var hIcon = await Task.Run(() => Icons.GetIconHandle(key, 32));
        var res = await ToBitmapImageAsync(hIcon);
        Api.DestroyIcon(hIcon);
        return res;
    }

    static async Task<BitmapImage?> ToBitmapImageAsync(nint hIcon)
    {
        using var icon = System.Drawing.Icon.FromHandle(hIcon);
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
/*
public static class IconCache
{
    public static BitmapImage IconFromResource(this string path)
    {
        var key = new CacheKey(path);

        if (cache.TryGetValue(key, out var image))
            return image;

        image = new BitmapImage(new Uri($"ms-appx:///{path}"));
        cache.Add(key, image);
        return image;
    }

    static readonly Dictionary<CacheKey, BitmapImage> cache = [];

    record CacheKey(string Path);
}*/