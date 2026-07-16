using Explorer.Controls;

using Microsoft.UI.Xaml.Media.Imaging;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FileCommander.Extensions;

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
}



public static class ShellIconCache
{
    private static readonly ConcurrentDictionary<CacheKey,
        Lazy<Task<BitmapSource?>>> _cache = new();

    public static Task<BitmapSource?> GetAsync(
        string? path,
        int size = 16)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult<BitmapSource?>(null);

        string key =
            path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? path
            : Path.GetExtension(path);

        if (string.IsNullOrEmpty(key))
            key = ".";

        var cacheKey = new CacheKey(key, size);

        return _cache.GetOrAdd(cacheKey,
            k => new Lazy<Task<BitmapSource?>>(
                () => LoadIconAsync(k))).Value;
    }

    public static void Clear()
    {
        _cache.Clear();
    }

    private static Task<BitmapSource?> LoadIconAsync(CacheKey key)
    {
        return LoadIconCoreAsync(key);
    }

    /// <summary>
    /// Implemented in Part 3.
    /// </summary>
    private static async Task<BitmapSource?> LoadIconCoreAsync(CacheKey key)
    {
        try
        {
            var hIcon = ShellIconInterop.GetIconHandle(".txt", 16);
            return await IconConverter.ToBitmapImageAsync(hIcon);
        }
        catch (Exception e)
        {
            throw;
        }
    }

    private readonly record struct CacheKey(
        string Extension,
        int Size);
}