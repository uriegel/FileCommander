using CsTools.Extensions;

using FileCommander.Data;

using System;
using System.IO;
using System.Linq;

namespace FileCommander.Controllers;

class DirectoryController : Controller
{
    public DirectoryController(string path)
    {
        this.path = path;
    }

    public override Column[] GetColumns()
        => [
            new("Name"),
            new("Datum"),
            new("Größe"),
            new("Version")
        ];

    public override ItemResult GetItems()
    {
        var dirInfo = new DirectoryInfo(path);
        path = dirInfo.FullName;
        dirItems = [.. dirInfo
            .GetDirectories()
            .Select(DirectoryItem.Create)
            .OrderBy(n => n.Name)];
        fileItems = [.. dirInfo
            .GetFiles()
            .Select(FileItem.Create)];
        var pos = fromPath != null ? dirItems.TakeWhile(n => n.Name != fromPath).Count() + 1 : 0;
        fromPath = null;
        return new([
             new Item(0, "..", "iconFromRes/GoUp", []),
            ..dirItems.Select((n, idx) => new Item(idx + 1, n.Name, n.GetIcon(), [ n.DateTime.ToString("g") ])),
            ..fileItems.Select((n, idx) => new Item(idx + dirItems.Length + 1, n.Name, n.GetIcon(path), [ n.DateTime.ToString("g"), n.Size.FormatSize() ]))
        ], pos);
    }

    public override OnProcessResult OnProcess(int pos)
    {
        if (pos == 0)
        {
            var info = new DirectoryInfo(path);
            if (info.Parent?.FullName is string p)
            {
                path = p;
                fromPath = info.Name;
                return new(NewItems: true);
            }
            else
                return new(NewController: new RootController(null));
        }
        else if (pos < dirItems.Length + 1)
        {
            path = path.AppendPath(dirItems[pos-1].Name);
            return new(NewItems: true);
        }
        return new OnProcessResult();
    }

    DirectoryItem[] dirItems = null!;
    FileItem[] fileItems = null!;

    string? fromPath = null;
    string path;
}

record DirectoryItem(string Name, DateTime DateTime)
{
    public static DirectoryItem Create(DirectoryInfo info)
        => new(info.Name, info.LastWriteTime);

    public string GetIcon() => "iconFromRes/Folder";
}

record FileItem(string Name, DateTime DateTime, long Size)
{
    public static FileItem Create(FileInfo info)
        => new(info.Name, info.LastWriteTime, info.Length);

    public string GetIcon(string path)
        => $"icon/{(Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? path.AppendPath(Name) : Name.GetFileExtension())}";
}

