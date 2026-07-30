using FileCommander.Data;

using System.IO;

namespace FileCommander.Controllers;

abstract class Controller
{
    //public abstract string Name { get; } 
    public static Controller GetFromPath(string? path, Controller? current)
    {
        if (path == null || path == "/.." || path.Length == 0 || path == RootController.NAME)
            return RootController.Get(current);
        else
            return RootController.Get(current);
        //return DirectoryController.Get(id, current, view, context);
    }
    public abstract Column[] GetColumns();
    public abstract (Item[] Items, string Path, int oldPos) GetItems(string path);
    public abstract (Controller Controller, Column[]? Columns, string Path, string OldPath) CheckPath(int pos);
    public virtual bool Process(int pos) => false;
}


