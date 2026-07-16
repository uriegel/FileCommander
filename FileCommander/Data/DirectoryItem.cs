using Explorer.Controls;

using FileCommander.Extensions;

using Microsoft.UI.Xaml.Media;

using System;
using System.IO;
using System.Threading.Tasks;

namespace FileCommander.Data;

public record DirectoryItem : Item
{
    public ImageSource? Icon { get; set; }

    public DateTime DateTime
    {
        get => field;
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
            Icon = "Resources/Folder.ico".IconFromResource(),
            IsHidden = info.Attributes.HasFlag(FileAttributes.Hidden),
            Name = info.Name ?? "",
            DateTime = info.LastWriteTime
        };

        var _ = Test();

        async Task Test()
        {
            var t = await ShellIconCache.GetAsync(".folder", 16);
            item.Icon = t;
        }


        return item;
    }



    public static DirectoryItem CreateFileItem(FileInfo info)
    {
        var item = new DirectoryItem()
        {
            Icon = "Resources/Folder.ico".IconFromResource(),
            IsHidden = info.Attributes.HasFlag(FileAttributes.Hidden),
            Name = info.Name ?? "",
            DateTime = info.LastWriteTime
        };

        var _ = Test();

        async Task Test()
        {
            var t = await ShellIconCache.GetAsync(".folder", 16);
            item.Icon = t;
        }


        return item;
    }
}


