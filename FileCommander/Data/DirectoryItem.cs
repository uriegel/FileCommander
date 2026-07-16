using CsTools.Extensions;

using FileCommander.Extensions;

using Microsoft.UI.Xaml.Media;

using System;
using System.IO;
using System.Threading.Tasks;

namespace FileCommander.Data;

public record DirectoryItem : Item
{
    public string? IconIndex 
    {
        get;
        set
        {
            field = value;
            OnChanged(nameof(IconIndex));
        }
    }

    public DateTime DateTime
    {
        get;
        set
        {
            field = value;
            OnChanged(nameof(DateTime));
        }
    }

    public static DirectoryItem Create(DirectoryInfo info)
    {
        var item = new DirectoryItem()
        {
            //Icon = "Resources/Folder.ico".IconFromResource(),
            IsHidden = info.Attributes.HasFlag(FileAttributes.Hidden),
            Name = info.Name ?? "",
            DateTime = info.LastWriteTime
        };
        return item;
    }



    public static DirectoryItem CreateFileItem(FileInfo info)
    {
        var item = new DirectoryItem()
        {
            IsHidden = info.Attributes.HasFlag(FileAttributes.Hidden),
            Name = info.Name ?? "",
            DateTime = info.LastWriteTime
        };

        var _ = GetIcon();
        return item;

        async Task GetIcon()
        {

            var ext = item.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) 
                    ? info.FullName
                    : item.Name.GetFileExtension();
            var _ = await ShellIconCache.GetAsync(ext);
            item.IconIndex = ext;
        }
    }
}


