using FileCommander.Data;

namespace FileCommander.Controllers;

abstract class Controller
{
    public static Controller GetFromPath(string? path, Controller? current)
    {
        if (path == null || path == "/.." || path.Length == 0 || path == RootController.Name)
            return RootController.Get(current);
        else
            return RootController.Get(current);
        //return DirectoryController.Get(id, current, view, context);
    }

    public Column[] GetColumns()
        => [
            new("Name"),
            new("Bezeichnung"),
            new("Größe")
        ];
}
