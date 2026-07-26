using CsTools.Extensions;

using System;
using System.IO;
using System.Threading.Tasks;

namespace FileCommander.Data;

public class FileItem : Item
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

    public long Size
    {
        get;
        set
        {
            field = value;
            OnChanged(nameof(Size));
        }
    }

    public static FileItem Create(FileInfo info)
    {
        var item = new FileItem()
        {
            IsHidden = info.Attributes.HasFlag(FileAttributes.Hidden) || info.Name.StartsWith(HiddenNamestart),
            Name = info.Name ?? "",
            Size = info.Length,
            DateTime = info.LastWriteTime
        };

        return item;
    }
}



