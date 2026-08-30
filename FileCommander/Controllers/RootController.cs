using CsTools;
using CsTools.Extensions;

using FileCommander.Contexts;
using FileCommander.Data;

using System.IO;
using System.Linq;

namespace FileCommander.Controllers;

class RootController : Controller
{
    public static string NAME { get => "root"; }
    
    public string Name { get; } = NAME;

    public static RootController Get(Controller? current, FolderContext context)
        => current is RootController rootController
            ? rootController
            : new RootController(context).SideEffect(_ => current?.Dispose());

    public RootController(FolderContext context) : base(context) { }

    public override Column[] GetColumns()
        => [
            new("Name"),
            new("Bezeichnung"),
            new("Größe", true)
        ];

    public override (Item[] Items, int oldPos, int dirCount, int fileCount) GetItems(
        string path, bool controllerChanged, bool fromHistory = false)
    {
        items =
           [.. DriveInfo
               .GetDrives()
               .Select(RootItem.Create)
               .OrderByDescending(n => n.IsMounted)
               .ThenBy(n => n.Name)];
        SetNewPath(Name, fromHistory);
        return ([.. items.Select(n => new Item(n.Name, n.GetIcon(), [
            n.Description,
            n.Size.FormatSize().EmptyWhen0()
          ], null, !n.IsMounted)), new(FavoriteController.NAME, "iconFromRes/Starred", ["Favoriten"])], 0, items.Length, 0);
    }

    public override (Controller Controller, Column[]? Columns, string Path, string OldPath) CheckPath(int pos)
    {
        var controller = pos == items.Length 
            ? (Controller)new FavoriteController(Context)
            : new DirectoryController(Context);
        var columns = controller.GetColumns();
        return (controller, columns, pos < items.Length ? items[pos].Name : "", Name);
    }
    
    public override string OnPosition(int pos) => items[pos].Name;
    
    public override (Item[] Items, int newPos, int dirs, int files) Reload(int pos)
    {
        var (items, _, dirs, files) = GetItems("", false);
        return (items, pos, dirs, files);
    }

    RootItem[] items = [];
}

record RootItem(string Name, string Description, long Size, bool IsMounted, bool IsRemovable) : ItemBase(Name, false)
{
    public static RootItem Create(DriveInfo driveInfo)
        => new(
            driveInfo.Name ?? "", 
            driveInfo.IsReady ? driveInfo.VolumeLabel : "", 
            driveInfo.IsReady ? driveInfo.TotalSize : 0, 
            driveInfo.IsReady,
            driveInfo.DriveType == DriveType.Removable);
    public override string GetIcon(string _ = "")
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
