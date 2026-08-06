using CsTools;

using FileCommander.Contexts;
using FileCommander.Data;

using System.IO;
using System.Linq;

using static System.Net.WebRequestMethods;

namespace FileCommander.Controllers;

class RootController : Controller
{
    public static string NAME { get => "root"; }
    
    public string Name { get; } = NAME;

    public static RootController Get(Controller? current, FolderContext context)
        => current is RootController rootController
            ? rootController
            : new RootController(context);

    public RootController(FolderContext context) : base(context) { }

    public override Column[] GetColumns()
        => [
            new("Name"),
            new("Bezeichnung"),
            new("Größe", true)
        ];

    public override (Item[] Items, string Path, int oldPos, int dirCount, int fileCount) GetItems(string path, bool controllerChanged)
    {
        items =
           [.. DriveInfo
               .GetDrives()
               .Select(RootItem.Create)
               .OrderByDescending(n => n.IsMounted)
               .ThenBy(n => n.Name)];
        Context.CurrentPath = Name;
        return ([.. items.Select(n => new Item(n.Name, n.GetIcon(), [
            n.Description,
            n.Size.FormatSize().EmptyWhen0()
          ], !n.IsMounted))], Name, 0, items.Length, 0);
    }

    public override (Controller Controller, Column[]? Columns, string Path, string OldPath) CheckPath(int pos)
    {
        var controller = new DirectoryController(Context);
        var columns = controller.GetColumns();
        return (controller, columns, items[pos].Name, Name);
    }
    
    public override string OnPosition(int pos) => items[pos].Name;
    
    public override (Item[] Items, int newPos, int dirs, int files) Reload(int pos)
    {
        var (items, _, _, dirs, files) = GetItems("", false);
        return (items, pos, dirs, files);
    }

    RootItem[] items = [];
}

record RootItem(string Name, string Description, long Size, bool IsMounted, bool IsRemovable)
{
    public static RootItem Create(DriveInfo driveInfo)
        => new(
            driveInfo.Name ?? "", 
            driveInfo.IsReady ? driveInfo.VolumeLabel : "", 
            driveInfo.IsReady ? driveInfo.TotalSize : 0, 
            driveInfo.IsReady,
            driveInfo.DriveType == DriveType.Removable);
    public string GetIcon()
        => "iconFromRes/" + (Name == @"C:\"
            ? "WindowsDrive"
            : IsRemovable
            ? "RemovableDrive"
            : "Drive");
}

static class ItemExtensions2
{
    public static string EmptyWhen0(this string value) => value == "0" ? "" : value;
}
