using CsTools.Extensions;

using FileCommander.Contexts;
using FileCommander.Data;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileCommander.Controllers;

class DirectoryController : Controller
{
    public override Column[] GetColumns()
        => [
            new("Name", SubColumn: "Erw.", Sortable: true),
            new("Datum", Sortable: true),
            new("Größe", true, Sortable: true),
            new("Version", Sortable: true)
        ];

    public override (Item[] Items, string Path, int oldPos, int dirCount, int fileCount) GetItems(string path)
    {
        var dirInfo = new DirectoryInfo(path);
        var dirItems = dirInfo
            .GetDirectories()
            .Select(DirectoryItem.Create)
            .ToArray();
        var fileItems = dirInfo
            .GetFiles()
            .Select(FileItem.Create)
            .ToArray();
        var fromPath = dirInfo.FullName.Length < this.path.Length ? this.path[dirInfo.FullName.Length..].Trim('\\') : null;
        this.path = dirInfo.FullName; 
        items = [new ParentItem(), .. dirItems, .. fileItems];
        (viewItems, var oldPos) = MapViewItems(fromPath);
        return (MapItems(), path, oldPos, viewItems.Count(n => n is DirectoryItem), viewItems.Count(n => n is FileItem));  
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

    public override string OnPosition(int pos) => path.AppendPath(viewItems[pos].Name);

    public override (Item[] Items, int newPos, int dirs, int files) Refresh(int pos)
    {
        var recentItem = viewItems[pos].Name;
        (viewItems, _) = MapViewItems(null);
        var newPos = viewItems.TakeWhile(n => n.Name != recentItem).Count();
        return (MapItems(), newPos < viewItems.Length ? newPos : 0, viewItems.Count(n => n is DirectoryItem), viewItems.Count(n => n is FileItem));
    }

    public override (Item[] Items, int newPos, int dirs, int files) Reload(int pos)
    {
        var recentItem = viewItems[pos].Name;
        var (items, _, _, dirs, files) = GetItems(path);
        var newPos = items.TakeWhile(n => n.Text != recentItem).Count();
        return (items, newPos < items.Length ? newPos : 0, dirs, files);
    }

    public override (Item[]? Items, int newPos) Sort(int index, bool descending, bool subcolumn, int pos)
    {
        sortIndex = index;
        sortDescending = descending;
        sortSubcolumn = subcolumn;
        var (items, newPos, _, _) = Refresh(pos);
        return (items, newPos);
    }

    (ItemBase[], int) MapViewItems(string? fromPath)
    {
        var filtered = items
            .Where(n => MainContext.Instance.ShowHidden || !n.IsHidden)
            .Order(new ItemsComparer(sortIndex, sortSubcolumn, sortDescending))
            .ToArray();
        var oldPos = fromPath != null ? filtered.TakeWhile(n => n.Name != fromPath).Count() : 0;
        return (filtered, oldPos);
    }

    Item[] MapItems()
        => [.. viewItems.Select(n =>

                n switch
                {
                    ParentItem p => new Item(p.Name, n.GetIcon(path), []),
                    DirectoryItem d => new Item(d.Name, n.GetIcon(path), [d.DateTime.ToString("g")], d.IsHidden),
                    FileItem f => new Item(f.Name, n.GetIcon(path), [f.DateTime.ToString("g"), f.Size.FormatSize()], f.IsHidden),
                    _ => throw new Exception("Unknown ItemBase")
                })];


    ItemBase[] items = null!;
    ItemBase[] viewItems = null!;

    string path = "";
    int sortIndex = -1;
    bool sortDescending = false;
    bool sortSubcolumn = false;
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

class ItemsComparer(int sortIndex, bool subColumn,  bool descending) : IComparer<ItemBase>
{
    public int Compare(ItemBase? x, ItemBase? y)
    {
        // Parent item always first
        int tx = x switch
        {
            ParentItem => 0,
            DirectoryItem => 1,
            _ => 2
        };

        int ty = y switch
        {
            ParentItem => 0,
            DirectoryItem => 1,
            _ => 2
        };

        int result = tx.CompareTo(ty);
        if (result != 0)
            return result;

        result = (sortIndex, subColumn) switch
        {
            (0, false) => StringComparer.CurrentCultureIgnoreCase.Compare(x!.Name, y!.Name),
            (0, true) => StringComparer.CurrentCultureIgnoreCase.Compare(x!.Name.GetFileExtension(), y!.Name.GetFileExtension()),
            (1, _) => CompareDate(x!, y!),
            (2, _) => CompareSize(x!, y!),
            _ => 0
        };

        return descending ? -result : result;
    }

    static int CompareDate(ItemBase x, ItemBase y)
    {
        DateTime dx = x switch
        {
            DirectoryItem d => d.DateTime,
            FileItem f => f.DateTime,
            _ => DateTime.MinValue
        };

        DateTime dy = y switch
        {
            DirectoryItem d => d.DateTime,
            FileItem f => f.DateTime,
            _ => DateTime.MinValue
        };

        return dx.CompareTo(dy);
    }

    static int CompareSize(ItemBase x, ItemBase y)
    {
        long sx = x is FileItem f1 ? f1.Size : -1;
        long sy = y is FileItem f2 ? f2.Size : -1;

        return sx.CompareTo(sy);
    }
}