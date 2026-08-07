using FileCommander.Contexts;
using FileCommander.Data;

using Windows.Storage;

namespace FileCommander.Controllers;

abstract class Controller
{
    public FolderContext Context { get; } = null!;

    public static Controller GetInitial(FolderContext context)
    {
        var settings = ApplicationData.Current.LocalSettings.Values;
        var key = $"{context.Id}-latestPath";
        var path = settings.ContainsKey(key) ? (string)settings[key] : null;
        return GetFromPath(path, null, context);
    }

    public static Controller GetFromPath(string? path, Controller? current, FolderContext context)
    {
        if (path == null || path == "/.." || path.Length == 0 || path == RootController.NAME)
            return RootController.Get(current, context);
        else
            return DirectoryController.Get(current, context); ;
    }

    protected Controller(FolderContext context) => Context = context; 

    protected void SetNewPath(string path, bool fromHistory = false)
    {
        if (!fromHistory)
            Context.AddHistory(path);
        Context.CurrentPath = path;
    }

    public abstract Column[] GetColumns();
    public abstract (Item[] Items, string Path, int oldPos, int dirCount, int fileCount) GetItems(string path, bool controllerChanged, bool fromHistory = false);
    public abstract string OnPosition(int pos);
    public virtual (Item[]? Items, int newPos, int dirs, int files) Refresh(int pos) => (null, 0, 0, 0);
    public abstract (Item[] Items, int newPos, int dirs, int files) Reload(int pos);
    public virtual (Item[]? Items, int newPos) Sort(int index, bool descending, bool subcolumn, int pos) => (null, 0);
    public abstract (Controller Controller, Column[]? Columns, string Path, string OldPath) CheckPath(int pos);
    public virtual bool Process(int pos) => false;
}


