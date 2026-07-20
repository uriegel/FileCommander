using CsTools.Extensions;

using FileCommander.Controls;
using FileCommander.Data;
using FileCommander.DataStore;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FileCommander.Controller;

class DirectoryController : IDisposable
{
    public DirectoryController(Store store, Context context)
    {
        this.store = store;
        this.context = context;

        watcher.Created += WatchCreated;
        watcher.Deleted += WatchDeleted;
        watcher.Changed += WatchChanged;
        watcher.Renamed += WatchRenamed;
        watcher.NotifyFilter = NotifyFilters.CreationTime
                    | NotifyFilters.DirectoryName
                    | NotifyFilters.FileName
                    | NotifyFilters.LastWrite
                    | NotifyFilters.Size;
    }

    public async Task ChangePathAsync(string path)
    {
        //var folderToSelect = path.EndsWith("..") ? context.CurrentPath.SubstringAfterLast('/') : null;
        //        cancellation.Cancel();
        //        cancellation = new();
        var items = await Get(path);
        var enableEvents = watcher.Path == "";
        watcher.Path = context.CurrentPath;
        if (enableEvents)
            watcher.EnableRaisingEvents = true;
        //view.OnItemsChange(true);
        //        store.Splice(0, store.ItemsCount(), items);
        store.Items.Clear();
        store.ChangeItems(items);
       
        //        StartExifResolving(items);
        //view.OnItemsChange(false);
        //int pos = folderToSelect != null
        //    ? model
        //        .GetItems<DirectoryItem>()
        //        .Select((n, i) => new DirItemPos(Item: n, Pos: i))
        //        .FirstOrDefault(n => n.Item.Name == folderToSelect)?.Pos
        //        ?? 0
        //    : 0;
        //SetSelection(pos);

        //MainContext.Instance.PropertyChanged -= OnPropertyChanged;
        //MainContext.Instance.PropertyChanged += OnPropertyChanged;
    }

    async Task<Item[]> Get(string path)
    {
        var dirInfo = new DirectoryInfo(path);
        var dirs = dirInfo
                        .GetDirectories()
                        .Select(DirectoryItem.Create)
                        .OrderBy(n => n.Name)
                        .ToArray();
        var files = dirInfo
                        .GetFiles()
                        .Select(FileItem.Create)
                        .ToArray();
        context.CurrentPath = dirInfo.FullName;
        //Application.Settings.SetString($"path-{Id}", dirInfo.FullName);
        return [
            new ParentItem(),
            .. dirs,
            .. files
        ];
    }

    void WatchCreated(object _, FileSystemEventArgs e)
    {
        try
        {
            //store.Splice(0, 0, [DirectoryItem.CreateFileItem(new FileInfo(e.FullPath))]);
            //view.CountsChanged(GetDirectoryCount(), GetFileCount());
        }
        catch { }
    }

    void WatchDeleted(object _, FileSystemEventArgs e)
    {
        //var pos = store.GetItems<DirectoryItem>().TakeWhile(n => n.Name != e.Name).Count();
        //store.Splice<DirectoryItem>(pos, 1, []);
        //view.CountsChanged(GetDirectoryCount(), GetFileCount());
    }

    void WatchChanged(object _, FileSystemEventArgs e)
    {
        var fileInfo = new FileInfo(context.CurrentPath.AppendPath(e.Name));
        var item = store.Items.FirstOrDefault(n => n.Name == e.Name);
        if (item is DirectoryItem di)
        {
            di.DateTime = fileInfo.LastWriteTime;
        }
        if (item is FileItem fi)
        {
            fi.DateTime = fileInfo.LastWriteTime;
            fi.Size = fileInfo.Length;
        }
    }

    void WatchRenamed(object _, RenamedEventArgs e)
    {
        //Console.WriteLine($"Renamed: {e.OldName} {e.Name}");
        //int focused = model.Selected;
        //var pos = model.GetItems<DirectoryItem>().TakeWhile(n => n.Name != e.OldName).Count();
        //bool focusNew = pos == focused;

        //var posToRemove = store.GetItems<DirectoryItem>().TakeWhile(n => n.Name != e.OldName).Count();
        //if (pos != store.GetItemsCount())
        //    store.Remove(posToRemove);

        //var fileInfo = new FileInfo(context.CurrentPath.AppendPath(e.Name));
        //if (!File.Exists(context.CurrentPath.AppendPath(e.Name)))
        //    store.Splice(0, 0, [DirectoryItem.CreateFileItem(fileInfo)]);
        //else
        //{
        //    var item = model.GetItems<DirectoryItem>().FirstOrDefault(n => n.Name == e.Name);
        //    item?.DateTime = fileInfo.LastWriteTime;
        //    item?.Size = fileInfo.Length;
        //}
        //view.CountsChanged(GetDirectoryCount(), GetFileCount());

        //if (focusNew)
        //{
        //    var newPos = model
        //        .GetItems<DirectoryItem>()
        //        .Select((n, i) => new DirItemPos(Item: n, Pos: i))
        //        .FirstOrDefault(n => n.Item.Name == e.Name)?.Pos;
        //    if (newPos.HasValue)
        //        SetSelection(newPos.Value);
        //}
    }

    Context context;
    readonly Store store;
    readonly FileSystemWatcher watcher = new();

    #region IDiposable
    
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // Verwalteten Zustand (verwaltete Objekte) bereinigen
                watcher.Dispose();
            }

            // Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
            // Große Felder auf NULL setzen
            disposedValue = true;
        }
    }

    // Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
    // ~DirectoryController()
    // {
    //     // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Ändern Sie diesen Code nicht. Fügen Sie Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    bool disposedValue;

    #endregion
}
