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

    public override (Item[], string) GetItems(string path)
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
        this.path = path;
        return ([
             new Item(0, "..", "iconFromRes/GoUp", []),
            ..dirItems.Select((n, idx) => new Item(idx + 1, n.Name, n.GetIcon(), [ n.DateTime.ToString("g") ])),
            ..fileItems.Select((n, idx) => new Item(idx + dirItems.Length + 1, n.Name, n.GetIcon(path), [ n.DateTime.ToString("g"), n.Size.FormatSize() ]))
        ], path);
    }

    public override (Controller Controller, Column[]? Columns, string Path, string OldPath) CheckPath(int pos)
    {
        return pos != 0
            ? (this, null, path.AppendPath(dirItems[pos - 1].Name), "")
            : new DirectoryInfo(path).Parent?.FullName is string newPath
            ? (this, null, newPath, path)
            : NewRootController(); 

        (Controller, Column[]?, string, string) NewRootController()
        {
            var controller = new RootController();
            return (controller, controller.GetColumns(), "", path);
        }
    }

    public override bool Process(int pos)
    {
        //if (pos == 0)
        //{
        //    var info = new DirectoryInfo(path);
        //    if (info.Parent?.FullName is string p)
        //    {
        //        path = p;
        //        fromPath = info.Name;
        //        return new(NewItems: true);
        //    }
        //    else
        //        return new(NewController: new RootController(null));
        //}
        if (pos > dirItems.Length)
            // TODO process item
            return true;
        //{
        //    
        //    return new(NewItems: true);
        else        
            return false;
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

