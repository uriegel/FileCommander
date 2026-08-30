using CsTools.Extensions;

using FileCommander.Contexts;
using FileCommander.Data;

using System;

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
        items = [ 
            new Item("..", "iconFromRes/GoUp", [ "" ]),
            new Item("Hinzufügen...", "iconFromRes/Plus", [ "" ])
        ];
        SetNewPath(Name, fromHistory);
        return (items, 0, items.Length - 2, 0);
    }

    public override string OnPosition(int pos) => items[pos].Text;
    
    public override (Item[] Items, int newPos, int dirs, int files) Reload(int pos)
    {
        var (items, oldPos, dir, file) = GetItems("", false);
        return (items, 0, items.Length - 2, 0);
    }
    
    public override (Controller Controller, Column[]? Columns, string Path, string OldPath) CheckPath(int pos)
    {
        var controller = pos == 0
            ? new RootController(Context)
            : new RootController(Context);
        var columns = controller.GetColumns();
        return (controller, columns, items[pos].Text, Name);
    }
    
    public FavoriteController(FolderContext context) : base(context) { }

    Item[] items = [];
}

