using FileCommander.Data;

using Microsoft.UI.Xaml.Media;

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

    public override Item[] GetItems()
    {
        items =
           [.. DriveInfo
               .GetDrives()
               .Select(RootItem.Create)
               .OrderByDescending(n => n.IsMounted)
               .ThenBy(n => n.Name)];
        return [.. items.Select((n, idx) => new Item(idx, n.Name, [
            new Value(StringVal: n.Description),
            new Value(LongVal: n.Size),
            new Value(BoolVal: n.IsMounted)
          ]))];
    }

    RootItem[] items = [];
}

public record RootItem(string Name, string Description, long Size, bool IsMounted)
{
    public static RootItem Create(DriveInfo driveInfo)
        => new(
            driveInfo.Name ?? "", 
            driveInfo.IsReady ? driveInfo.VolumeLabel : "", 
            driveInfo.IsReady ? driveInfo.TotalSize : 0, 
            driveInfo.IsReady);

    static string GetIcon(DriveInfo driveInfo)
        => driveInfo.Name == @"C:\"
            ? "WindowsDrive"
            : driveInfo.DriveType == DriveType.Removable
            ? "RemovableDrive"
            : "Drive";
}
//Icon = $"Resources/{GetIcon(driveInfo)}.ico".IconFromResource(),
