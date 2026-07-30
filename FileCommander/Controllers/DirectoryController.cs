using CsTools.Extensions;

using FileCommander.Data;

using System;
using System.IO;
using System.Linq;

namespace FileCommander.Controllers;

class DirectoryController : Controller
{
    public override Column[] GetColumns()
        => [
            new("Name"),
            new("Datum"),
            new("Größe"),
            new("Version")
        ];

    public override (Item[], string, int) GetItems(string path)
    {
        var dirInfo = new DirectoryInfo(path);
        var dirItems = dirInfo
            .GetDirectories()
            .Select(DirectoryItem.Create)
            .OrderBy(n => n.Name)
            .ToArray();
        var fileItems = dirInfo
            .GetFiles()
            .Select(FileItem.Create)
            .ToArray();
        var fromPath = dirInfo.FullName.Length < this.path.Length ? this.path[dirInfo.FullName.Length..].Trim('\\') : null;
        this.path = dirInfo.FullName; 
        items = [new ParentItem(), .. dirItems, .. fileItems];
        var oldPos = fromPath != null ? items.TakeWhile(n => n.Name != fromPath).Count() : 0;
        return (items
            .Select((n, idx) =>
                n switch
                {
                    ParentItem p => new Item(idx, p.Name, n.GetIcon(path), []),
                    DirectoryItem d => new Item(idx, d.Name, n.GetIcon(path), [d.DateTime.ToString("g")]),
                    FileItem f => new Item(idx, f.Name, n.GetIcon(path), [f.DateTime.ToString("g"), f.Size.FormatSize()]),
                    _ => throw new Exception("Unknown ItemBase")
                })
            .ToArray(), path, oldPos);
    }

    public override (Controller Controller, Column[]? Columns, string Path, string OldPath) CheckPath(int pos)
    {
        return pos != 0
            ? (this, null, path.AppendPath(items[pos].Name), "")
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
        if (pos >= items.TakeWhile(n => n is not FileItem).Count())
            // TODO process item
            return true;
        else        
            return false;
    }

    ItemBase[] items = null!;
    ItemBase[] ViewItems = null!;

    string path = "";
}


abstract record ItemBase(string Name)
{
    public abstract string GetIcon(string path);
}
record ParentItem(string Name = "..") : ItemBase(Name)
{
    public override string GetIcon(string path) => "iconFromRes/GoUp";
}
record DirectoryItem(string Name, DateTime DateTime) : ItemBase(Name)
{
    public static DirectoryItem Create(DirectoryInfo info) => new(info.Name, info.LastWriteTime);
    public override string GetIcon(string path) => "iconFromRes/Folder";
}
record FileItem(string Name, DateTime DateTime, long Size) : ItemBase(Name)
{
    public static FileItem Create(FileInfo info) => new(info.Name, info.LastWriteTime, info.Length);
    public override string GetIcon(string path)
        => $"icon/{(Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? path.AppendPath(Name) : Name.GetFileExtension())}";
}

