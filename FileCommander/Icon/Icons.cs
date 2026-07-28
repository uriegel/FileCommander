using ClrWinApi;

using System;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace FileCommander.Icon;

static class Icons
{
    public static Stream Get(string path)
    {
        var hIcon = GetIconHandle(path, 16);
        using var icon = System.Drawing.Icon.FromHandle(hIcon);
        using var bitmap = icon.ToBitmap();
        var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        Api.DestroyIcon(hIcon);
        return stream;
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
}
