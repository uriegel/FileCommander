using CsTools.Extensions;

using FileCommander.Contexts;
using FileCommander.Data;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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

    public override async Task<(Item[] Items, int oldPos, int dirCount, int fileCount)> GetItemsAsync(
        string path, bool controllerChanged, bool fromHistory = false)
    {
        items =
           [ new RootItem(System.IO.Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.Personal))?.FullName ?? "",
                "Start",0, true, false),
            .. DriveInfo
               .GetDrives()
               .Select(RootItem.Create)
               .OrderByDescending(n => n.IsMounted)
               .ThenBy(n => n.Name)];
        SetNewPath(Name, fromHistory);
        return ([.. items.Select((n, i) => new Item(n.Name, i == 0 ? "iconFromRes/Home" : n.GetIcon(), [
            n.Description,
            n.Size.FormatSize().EmptyWhen0()
          ], null, !n.IsMounted)), new(FavoriteController.NAME, "iconFromRes/Starred", ["Favoriten"])], 0, items.Length, 0);
    }

    public override (Controller Controller, Column[]? Columns, string Path, string OldPath) CheckPath(int pos)
    {
        var controller = pos == 0
            ? new DirectoryController(Context)
            : pos == items.Length
            ? (Controller)new FavoriteController(Context)
            : new DirectoryController(Context);
        var columns = controller.GetColumns();
        return (controller, columns, pos < items.Length ? items[pos].Name : "", Name);
    }
    
    public override string OnPosition(int pos) => items[pos].Name;
    
    public override async Task<(Item[] Items, int newPos, int dirs, int files)> ReloadAsync(int pos)
    {
        var (items, _, dirs, files) = await GetItemsAsync("", false);
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
