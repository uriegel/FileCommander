using System;
using System.IO;
using System.Linq;

using CsTools.Extensions;

using FileCommander.Data;

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

    public override Item[] GetItems()
    {
        var dirInfo = new DirectoryInfo(path);
        path = dirInfo.FullName;
        var parentItem = new ParentItem();
        var dirs = dirInfo
            .GetDirectories()
            .Select(DirectoryItem.Create)
            .OrderBy(n => n.Name)
            .ToArray();
        var files = dirInfo
            .GetFiles()
            .Select(FileItem.Create)
            .ToArray();
        return [
             new Item(0, "..", "iconFromRes/GoUp", []),
            ..dirs.Select((n, idx) => new Item(idx + 1, n.Name, n.GetIcon(), [ n.DateTime.ToString("g") ])),
            ..files.Select((n, idx) => new Item(idx + dirs.Length + 1, n.Name, n.GetIcon(path), [ n.DateTime.ToString("g"), n.Size.FormatSize() ]))
        ];
    }

    public override OnProcessResult OnProcess(int pos)
    {
        return new();
    }

    string path;
}

record ParentItem();

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
