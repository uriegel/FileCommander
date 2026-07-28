using System;
using System.IO;
using System.Linq;

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
        var dirs = dirInfo
                        .GetDirectories()
                        .Select(DirectoryItem.Create)
                        .OrderBy(n => n.Name);
        return [.. dirs.Select((n, idx) => new Item(idx, n.Name, n.GetIcon(), [ n.DateTime.ToString("g") ]))];
        //var files = dirInfo
        //                .GetFiles()
        //                .Select(FileItem.Create)
        //                .Select(n => new VTItem(
        //                    $"icon/{(n.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? dirInfo.FullName.AppendPath(n.Name) : n.Name.GetFileExtension())}",
        //                    n.Name,
        //                    n.Size.FormatSize(),
        //                    n.DateTime.ToString("g")))
        //                .ToArray();
        //return [
        //    new VTItem(null, "..", null, null),
        //    .. dirs,
        //    .. files
        //];
        return [];
    }

    public override OnProcessResult OnProcess(int pos)
    {
        return new();
    }

    string path;
}

record DirectoryItem(string Name, DateTime DateTime)
{
    public static DirectoryItem Create(DirectoryInfo info)
        => new(info.Name, info.LastWriteTime);

    public string GetIcon() => "iconFromRes/Folder";
}
