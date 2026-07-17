using ClrWinApi;

using Microsoft.UI.Xaml.Media.Imaging;

using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
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
        var hIcon = GetIconHandle(key, 16);
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

    static nint GetIconHandle(string pathOrExtension, int size)
    {
        var info = new ShFileInfo();
        var result = Api.SHGetFileInfo(pathOrExtension, ClrWinApi.FileAttributes.Normal, ref info, Marshal.SizeOf<ShFileInfo>(),
            SHGetFileInfoConstants.SYSICONINDEX | SHGetFileInfoConstants.USEFILEATTRIBUTES | SHGetFileInfoConstants.TYPENAME);
        if (result == 0)
            return 0;

        var imageListSize = size switch
        {
            <= 16 => ShilImageListSize.Small,
            <= 32 => ShilImageListSize.Large,
            <= 48 => ShilImageListSize.ExtraLarge,
            _ => ShilImageListSize.Jumbo
        };

        var guid = Guids.IID_IImageList;
        Api.SHGetImageList(imageListSize, ref guid, out IImageList? imageList);
        if (imageList == null)
            return 0;

        imageList.GetIcon(info.Icon, ImageListDrawFlags.Transparent, out IntPtr hIcon);
        return hIcon;
    }

    static readonly ConcurrentDictionary<string, Lazy<Task<BitmapSource?>>> cache = [];
}
