using CsTools;

using FileCommander.Data;

using System.IO;
using System.Linq;

namespace FileCommander.Controllers;

class RootController : Controller
{
    public static string NAME { get => "root"; }
    
    public string Name { get; } = NAME;

    public static RootController Get(Controller? current)
        => current is RootController rootController
            ? rootController
            : new RootController();

    public RootController() { }

    public override Column[] GetColumns()
        => [
            new("Name"),
            new("Bezeichnung"),
            new("Größe", true)
        ];

    public override (Item[] Items, string Path, int oldPos) GetItems(string path)
    {
        items =
           [.. DriveInfo
               .GetDrives()
               .Select(RootItem.Create)
               .OrderByDescending(n => n.IsMounted)
               .ThenBy(n => n.Name)];
        return ([.. items.Select(n => new Item(n.Name, n.GetIcon(), [
            n.Description,
            n.Size.FormatSize().EmptyWhen0()
          ], !n.IsMounted))], Name, 0);
    }

    public override (Controller Controller, Column[]? Columns, string Path, string OldPath) CheckPath(int pos)
    {
        var controller = new DirectoryController();
        var columns = controller.GetColumns();
        return (controller, columns, items[pos].Name, Name);
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
