using CsTools;
using CsTools.Extensions;

using FileCommander.Data;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace FileCommander.Controllers;

// TODO: after getItems call refresh?extended=true
    // => extended items and finished true/false
// TODO: Cancellation
// TODO: When restriction


class ExtendedFileInfos
{
    public async void Get(string path, IEnumerable<FileItem> items, Action<Event>? SendEvent)
    {
        var extendedItems = GetExtendedFileItems(items);
        if (extendedItems.Length > 0)
            await foreach (var extendedItem in extendedItems.ToAsyncEnumerable()) 
            {
                if (extendedItem.Exif)
                {
                    var info = await Task.Run(() =>
                    {
                        return ExifReader.GetExifData(path.AppendPath(extendedItem.Item.Name));
                    });
                    if (info != null)
                        extendedItem.Item.ExifData = info;
                }
            }
    }

    static ExtendedFileItem[] GetExtendedFileItems(IEnumerable<FileItem> items)
        => [.. items.Select(n => new ExtendedFileItem(IsVersion(n), IsExif(n), n))];


    static bool IsExif(FileItem item)
    {
        var ext = item.Name.GetFileExtension();
        return string.CompareOrdinal(item.Name.GetFileExtension(), ".jpg") == 0
           || string.CompareOrdinal(item.Name.GetFileExtension(), ".png") == 0;
    }

    static bool IsVersion(FileItem item)
    {
        var ext = item.Name.GetFileExtension();
        return string.CompareOrdinal(item.Name.GetFileExtension(), ".exe") == 0
           || string.CompareOrdinal(item.Name.GetFileExtension(), ".dll") == 0;
    }

    Task? task;
}

record ExtendedFileItem(bool Version, bool Exif, FileItem Item);
