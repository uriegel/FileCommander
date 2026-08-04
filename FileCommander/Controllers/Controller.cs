using FileCommander.Data;

using System.IO;
using System.Reflection;

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
    public abstract (Item[] Items, string Path, int oldPos, int dirCount, int fileCount) GetItems(string path);
    public abstract string OnPosition(int pos);
    public virtual (Item[]? Items, int newPos, int dirs, int files) Refresh(int pos) => (null, 0, 0, 0);
    public abstract (Item[] Items, int newPos, int dirs, int files) Reload(int pos);
    public virtual (Item[]? Items, int newPos) Sort(int index, bool descending, bool subcolumn, int pos) => (null, 0);
    public abstract (Controller Controller, Column[]? Columns, string Path, string OldPath) CheckPath(int pos);
    public virtual bool Process(int pos) => false;
}


