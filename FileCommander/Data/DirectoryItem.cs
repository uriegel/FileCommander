using System;
using System.IO;

namespace FileCommander.Data;

public class DirectoryItem : Item
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

}


