using CsTools.Extensions;

using FileCommander.Contexts;
using FileCommander.Data;

using Microsoft.UI.Xaml.Shapes;

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
        (viewItems, var oldPos) = MapItems(fromPath);
        return (viewItems.Select((n, idx) =>
                n switch
                {
                    ParentItem p => new Item(idx, p.Name, n.GetIcon(path), []),
                    DirectoryItem d => new Item(idx, d.Name, n.GetIcon(path), [d.DateTime.ToString("g")]),
                    FileItem f => new Item(idx, f.Name, n.GetIcon(path), [f.DateTime.ToString("g"), f.Size.FormatSize()]),
                    _ => throw new Exception("Unknown ItemBase")
                }).ToArray(), path, oldPos);  
    }

    public override (Controller Controller, Column[]? Columns, string Path, string OldPath) CheckPath(int pos)
    {
        return pos != 0
            ? (this, null, path.AppendPath(viewItems[pos].Name), "")
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
        if (pos >= viewItems.TakeWhile(n => n is not FileItem).Count())
            // TODO process item
            return true;
        else        
            return false;
    }

    (ItemBase[], int) MapItems(string? fromPath)
    {
        var filtered = items
            .Where(n => MainContext.Instance.ShowHidden || !n.IsHidden)
            .ToArray();
        var oldPos = fromPath != null ? filtered.TakeWhile(n => n.Name != fromPath).Count() : 0;
        return (filtered, oldPos);
    }

    ItemBase[] items = null!;
    ItemBase[] viewItems = null!;

    string path = "";
}

abstract record ItemBase(string Name, bool IsHidden)
{
    public abstract string GetIcon(string path);
}
record ParentItem() : ItemBase("..", false)
{
    public override string GetIcon(string path) => "iconFromRes/GoUp";
}
record DirectoryItem(string Name, bool IsHidden, DateTime DateTime) : ItemBase(Name, IsHidden)
{
    public static DirectoryItem Create(DirectoryInfo info) => new(info.Name, info.IsHidden(), info.LastWriteTime);
    public override string GetIcon(string path) => "iconFromRes/Folder";
}
record FileItem(string Name, bool IsVisible, DateTime DateTime, long Size) : ItemBase(Name, IsVisible)
{
    public static FileItem Create(FileInfo info) => new(info.Name, info.IsHidden(), info.LastWriteTime, info.Length);
    public override string GetIcon(string path)
        => $"icon/{(Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? path.AppendPath(Name) : Name.GetFileExtension())}";
}

static class ItemExtensions
{
    public static bool IsHidden(this FileSystemInfo info)
        => (info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden || info.Name.StartsWith('.');
}
