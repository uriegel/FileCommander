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

