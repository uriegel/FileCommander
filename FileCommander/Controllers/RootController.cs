using FileCommander.Data;

using System.IO;
using System.Linq;

namespace FileCommander.Controllers;

class RootController : Controller
{
    public const string Name = "root";

    public static RootController Get(Controller? current)
        => current is RootController rootController
            ? rootController
            : new RootController(current);

    public RootController(Controller? previous) { }

    public override Column[] GetColumns()
        => [
            new("Name"),
            new("Bezeichnung"),
            new("Größe")
        ];

    public override ItemResult GetItems()
    {
        items =
           [.. DriveInfo
               .GetDrives()
               .Select(RootItem.Create)
               .OrderByDescending(n => n.IsMounted)
               .ThenBy(n => n.Name)];
        return new([.. items.Select((n, idx) => new Item(idx, n.Name, n.GetIcon(), [
            n.Description,
            n.Size.FormatSize()
          ]))], 0);
    }

    public override OnProcessResult OnProcess(int pos)
    {
        return new(NewController: new DirectoryController(items[pos].Name));
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

