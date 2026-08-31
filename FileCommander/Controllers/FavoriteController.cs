using CsTools.Extensions;

using FileCommander.Contexts;
using FileCommander.Controls;
using FileCommander.Data;

using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Windows.Storage;

namespace FileCommander.Controllers;

class FavoriteController : Controller
{
    public static string NAME { get => "fav"; }

    public string Name { get; } = NAME;

    public static FavoriteController Get(Controller? current, FolderContext context)
        => current is FavoriteController favController
            ? favController
            : new FavoriteController(context).SideEffect(_ => current?.Dispose());

    public override Column[] GetColumns() => [
            new("Name"),
            new("Pfad"),
        ];

    public override (Item[] Items, int oldPos, int dirCount, int fileCount) GetItems(string path, bool controllerChanged, bool fromHistory = false)
    {
        var settings = ApplicationData.Current.LocalSettings.Values;
        var favs = settings["Favorites"] is string favstr ? JsonSerializer.Deserialize<Favorite[]>(favstr) ?? [] : [];
        items = [ 
            new Item("..", "iconFromRes/GoUp", [ "" ]),
            .. favs.Select(n => new Item(n.Name, "iconFromRes/Starred", [ n.Path ], IsSelectable: true)).OrderBy(n => n.Text),
            new Item("Hinzufügen...", "iconFromRes/Plus", [ "" ])
        ];
        SetNewPath(Name, fromHistory);
        return (items, 0, items.Length - 2, 0);
    }

    public override string OnPosition(int pos) => items[pos].Values[0];
    
    public override (Item[] Items, int newPos, int dirs, int files) Reload(int pos)
    {
        var (items, _, _, _) = GetItems("", false);
        return (items, 0, items.Length - 2, 0);
    }
    
    public override (Controller Controller, Column[]? Columns, string Path, string OldPath) CheckPath(int pos)
    {
        Controller controller = pos == 0
            ? new RootController(Context)
            : pos == items.Length - 1
            ? this.SideEffect(_ => AddFavorite())
            : items[pos].Values[0] == RootController.NAME ? new RootController(Context) : new DirectoryController(Context);
        var columns = controller.GetColumns();
        return (controller, columns, items[pos].Values[0], Name);
    }

    public override async Task<bool> DeleteItems(int[] itemsPos)
    {
        var toDelete = items.Where((n, i) => itemsPos.Contains(i)).ToArray();
        if (await Dialog.ShowAsync(MainWindow.Content,
            "Favoriten löschen",
            textContent: $"Möchtest du {(toDelete.Length == 1 ? "den" : "die")} Favoriten löschen?"))
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            var favs = settings["Favorites"] is string favstr ? JsonSerializer.Deserialize<Favorite[]>(favstr) ?? [] : [];
            settings["Favorites"] = JsonSerializer.Serialize<Favorite[]>([.. favs.Where(n => !toDelete.Any(m => m.Values[0] == n.Path))]);
            MainWindow.Refresh();
            return true;
        }
        else
            return false;
    }

    public override async Task<bool> Rename(int pos, bool asCopy)
    {
        var item = items[pos];
        var newName = await Dialog.ShowAsync(MainWindow.Content, "Umbenennen",
            d => (d.Content as RenameDialog)?.FileName ?? "",
            new RenameDialog()
            {
                Description = "Möchtest du den Favoriten umbenennen?",
                FileName = item.Text
            });
        if (newName != null)
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            var favs = settings["Favorites"] is string favstr ? JsonSerializer.Deserialize<Favorite[]>(favstr) ?? [] : [];
            settings["Favorites"] = JsonSerializer.Serialize<Favorite[]>(
                [.. favs.Select(n => n.Path == item.Values[0] ? new Favorite(newName, item.Values[0]) : n)]
            );
            MainWindow.Refresh();
            return true;
        }
        else
            return false;
    }

    public FavoriteController(FolderContext context) : base(context) { }

    async void AddFavorite()
    {
        var otherContext = MainWindow.GetOtherContext(Context);
        var res = await Dialog.ShowAsync(MainWindow.Content, "Favoriten anlegen",
            d => new Favorite((d.Content as NewFavorite)?.FavoriteName ?? "", (d.Content as NewFavorite)?.Path ?? ""),
            new NewFavorite()
            {
                FavoriteName = otherContext.CurrentPath,
                Path = otherContext.CurrentPath,
            });
        if (res != null)
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            var favs = settings["Favorites"] is string favstr ? JsonSerializer.Deserialize<Favorite[]>(favstr) ?? [] : [];
            settings["Favorites"] = JsonSerializer.Serialize<Favorite[]>([ ..favs, res ]);
            MainWindow.Refresh();
        }
    }

    Item[] items = [];
}

