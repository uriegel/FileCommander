using System.Diagnostics;

using CsTools;

using FileCommander.Controllers;

namespace FileCommander.Data;

record Item(string Text, string Icon, string[] Values, ExifValue? ExifValue = null, bool Hidden = false, bool IsSelectable = false)
{
    public static Item Get(FileItem item, string path)
        => new(item.Name, item.GetIcon(path), [item.DateTime.ToString("g"), item.Size.FormatSize(), item.Version?.Format() ?? ""], 
            ExifValue.Create(item.ExifData), item.IsHidden, true);

    public static Item Get(DirectoryItem item)
        => new(item.Name, item.GetIcon(""), [item.DateTime.ToString("g"), "", ""], null, item.IsHidden, true);
};

record ExifValue(string? Date, double? Latitude, double? Longitude)
{
    public static ExifValue? Create(ExifData? data)
        => data is null
            ? null
            : new ExifValue(data.DateTime?.ToString("g"), data.Latitude, data.Longitude);

}

record ItemsResult(
    Column[]? Columns,
    Item[] Items, 
    int Pos);

static class ItemExtensions
{
    public static string FormatSize(this long size)
    {
        if (size == -1)
            return "";
        var sizeStr = size.ToString();
        var sep = '.';
        if (sizeStr.Length > 3)
        {
            var sizePart = sizeStr;
            sizeStr = "";
            for (var j = 3; j < sizePart.Length; j += 3)
            {
                var extract = sizePart.Substring(sizePart.Length - j, 3);
                sizeStr = sep + extract + sizeStr;
            }
            var strfirst = sizePart[..((sizePart.Length % 3 == 0) ? 3 : (sizePart.Length % 3))];
            sizeStr = strfirst + sizeStr;
        }
        return sizeStr;
    }

    public static string Format(this FileVersionInfo version)
        => $"{version.FileMajorPart}.{version.FileMinorPart}.{version.FileBuildPart}.{version.FilePrivatePart}";
}

