using CsTools.Extensions;

using FileCommander.Contexts;
using FileCommander.Controls;
using FileCommander.Data;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FileCommander.Controllers;

class DirectoryController : Controller
{
    public static DirectoryController Get(Controller? current, FolderContext context)
    => current is DirectoryController dirController
        ? dirController
        : new DirectoryController(context).SideEffect(_ => current?.Dispose());

    public DirectoryController(FolderContext context) : base(context) 
    {
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

    public override Column[] GetColumns()
        => [
            new("Name", SubColumn: "Erw.", Sortable: true),
            new("Datum", Sortable: true),
            new("Größe", true, Sortable: true),
            new("Version", Sortable: true)
        ];

    public override (Item[] Items, int oldPos, int dirCount, int fileCount) GetItems(
        string path, bool controllerChanged, bool fromHistory = false)
    {
        var dirInfo = new DirectoryInfo(path);
        var dirItems = dirInfo
            .GetDirectories()
            .Select(DirectoryItem.Create)
            .ToArray();
        var fileItems = dirInfo
            .GetFiles()
            .Select(FileItem.Create)
            .ToArray();
        var fromPath = !controllerChanged && dirInfo.FullName.Length < Context.CurrentPath.Length ? Context.CurrentPath[dirInfo.FullName.Length..].Trim('\\') : null;
        SetNewPath(dirInfo.FullName, fromHistory);
        items = [new ParentItem(), .. dirItems, .. fileItems];
        changes?.Dispose();
        changes = new();
        extendedFileInfos?.Dispose();
        extendedFileInfos = new(changes, Context.CurrentPath, Context, items.SelectFilterNull(n => n as FileItem));
        (viewItems, var oldPos) = MapViewItems(fromPath);

        var enableEvents = watcher.Path == "";
        watcher.Path = Context.CurrentPath;
        if (enableEvents)
            watcher.EnableRaisingEvents = true;

        return (MapItems(), oldPos, viewItems.Count(n => n is DirectoryItem), viewItems.Count(n => n is FileItem));  
    }

    public override (Controller Controller, Column[]? Columns, string Path, string OldPath) CheckPath(int pos)
    {
        return pos != 0
            ? (this, null, Context.CurrentPath.AppendPath(viewItems[pos].Name), "")
            : new DirectoryInfo(Context.CurrentPath).Parent?.FullName is string newPath
            ? (this, null, newPath, Context.CurrentPath)
            : NewRootController(); 

        (Controller, Column[]?, string, string) NewRootController()
        {
            var controller = new RootController(Context);
            return (controller, controller.GetColumns(), "", Context.CurrentPath);
        }
    }

    public override bool Process(int pos)
    {
        if (pos >= viewItems.TakeWhile(n => n is not FileItem).Count())
            // TODO process item
            return true;
        else        
            return false;
    }

    public override async Task<Change[]?> GetItemChangesAsync() 
            => await (changes?.GetItemsAsync() 
                ?? Task.FromResult<Change[]?>(null));

    public override string OnPosition(int pos) 
        => pos < viewItems.Length ? Context.CurrentPath.AppendPath(viewItems[pos].Name) : Context.CurrentPath;

    public override (Item[] Items, int newPos, int dirs, int files) Refresh(int pos)
    {
        var recentItem = viewItems[pos].Name;
        (viewItems, _) = MapViewItems(null);
        var newPos = viewItems.TakeWhile(n => n.Name != recentItem).Count();
        return (MapItems(), newPos < viewItems.Length ? newPos : 0, viewItems.Count(n => n is DirectoryItem), viewItems.Count(n => n is FileItem));
    }

    public override (Item[] Items, int newPos, int dirs, int files) Reload(int pos)
    {
        var recentItem = viewItems[pos].Name;
        var (items, _, dirs, files) = GetItems(Context.CurrentPath, false);
        var newPos = items.TakeWhile(n => n.Text != recentItem).Count();
        return (items, newPos < items.Length ? newPos : 0, dirs, files);
    }

    public override (Item[]? Items, int newPos) Sort(int index, bool descending, bool subcolumn, int pos)
    {
        sortIndex = index;
        sortDescending = descending;
        sortSubcolumn = subcolumn;
        var (items, newPos, _, _) = Refresh(pos);
        return (items, newPos);
    }

    public override async void CreateFolder(UIElement content) 
    {
        var dialog = new ContentDialog
        {
            Title = "Ordner anlegen",
            Content = new CreateFolderDialog()
            {
                FolderName = Context.SelectedPath.EndsWith("..") == false ? Context.SelectedPath.SubstringAfterLast('\\') : ""
            },
            PrimaryButtonText = "Ok",
            CloseButtonText = "Abbrechen",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = content.XamlRoot
        };
        var result = await dialog.ShowAsync();


        //if (result == ContentDialogResult.Primary)
        //{
        //    // Delete the file
        //}
    }

    (ItemBase[], int) MapViewItems(string? fromPath)
    {
        var filtered = items
            .Where(n => MainContext.Instance.ShowHidden || !n.IsHidden)
            .Order(new ItemsComparer(sortIndex, sortSubcolumn, sortDescending))
            .ToArray();
        var oldPos = fromPath != null ? filtered.TakeWhile(n => n.Name != fromPath).Count() : 0;
        return (filtered, oldPos);
    }

    void WatchCreated(object _, FileSystemEventArgs e)
    {
        Debug.WriteLine($"Created: {e.Name}");
        try
        {
            var isFile = File.Exists(e.FullPath);
            var newItem = isFile 
                ? (ItemBase)FileItem.Create(new FileInfo(e.FullPath)) 
                : DirectoryItem.Create(new DirectoryInfo(e.FullPath));
            items = [
                newItem,
                .. items
                ];
            (viewItems, _) = MapViewItems(null);
            MainWindow.RunOnUI(() =>
            {
                if (isFile)
                    Context.CurrentFileCount = Context.CurrentFileCount + 1;
                else
                    Context.CurrentDirectoryCount = Context.CurrentDirectoryCount + 1;
            });
            var pos = viewItems.TakeWhile(n => n.Name != e.Name).Count();
            
            var selectedItem = Context.SelectedPath.SubstringAfterLast('\\');
            var selpos = viewItems.TakeWhile(n => n.Name != selectedItem).Count();
            if (selpos == viewItems.Length)
                selpos = 0;

            var item = isFile 
                ? Item.Get((newItem as FileItem)!, Context.CurrentPath)
                : Item.Get((newItem as DirectoryItem)!);
            changes?.AddCreateItem(item, pos, selpos);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Could not create changed: {ex}");
        }
    }

    void WatchDeleted(object _, FileSystemEventArgs e)
    {
        Debug.WriteLine($"Deleted: {e.Name}");
        try
        {
            var selectedItem = Context.SelectedPath.SubstringAfterLast('\\');
            var pos = viewItems.TakeWhile(n => n.Name != selectedItem).Count();
            if (pos == viewItems.Length)
                pos = 0;
            var delPos = viewItems.TakeWhile(n => n.Name != e.Name).Count();
            var isFile = items.FirstOrDefault(n => n.Name == e.Name) is FileItem;
            items = [.. items.Where(n => n.Name != e.Name)];
            (viewItems, _) = MapViewItems(null);
            MainWindow.RunOnUI(() =>
            {
                if (isFile)
                    Context.CurrentFileCount = Context.CurrentFileCount - 1;
                else
                    Context.CurrentDirectoryCount = Context.CurrentDirectoryCount - 1;
            });
            if (pos > delPos)
                pos--;
            changes?.AddDeletedItem(delPos, pos);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Could not delete changed: {ex}");
        }
    }

    void WatchRenamed(object _, RenamedEventArgs e)
    {
        try
        {
            var oldItem = items.FirstOrDefault(n => n.Name == e.OldName);
            if (oldItem == null)
            {
                WatchCreated(this, new FileSystemEventArgs(WatcherChangeTypes.Created, e.FullPath, e.Name));
                return;
            }
            var oldPos = Array.IndexOf(viewItems, oldItem);
            var newItem = oldItem with { Name = e.Name ?? "" };
            items = [
                newItem,
                .. items.Where(n => n.Name != e.OldName)
            ]; var selectedItem = Context.SelectedPath.SubstringAfterLast('\\');
            (viewItems, _) = MapViewItems(null);

            var selpos = viewItems.TakeWhile(n => n.Name != selectedItem).Count();
            if (selpos == viewItems.Length)
                selpos = 0;
            var newPos = Array.IndexOf(viewItems, newItem);
            
            var item = oldItem is FileItem
                ? Item.Get((newItem as FileItem)!, Context.CurrentPath)
                : Item.Get((newItem as DirectoryItem)!);
            changes?.AddRenameItem(item, oldPos, newPos, selpos);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Could not rename changed: {ex}");
        }
    }

    void WatchChanged(object _, FileSystemEventArgs e)
    {
        Debug.WriteLine($"Changed: {e.Name}");
        var item = items.FirstOrDefault(n => n.Name == e.Name);
        if (item is DirectoryItem di)
        {
            var dirInfo = new DirectoryInfo(e.FullPath);
            di.DateTime = dirInfo.LastWriteTime;
            changes?.AddChangedItem(Item.Get(di));
        }
        else if (item is FileItem fi)
        {
            var fileInfo = new FileInfo(e.FullPath);
            fi.DateTime = fileInfo.LastWriteTime;
            fi.Size = fileInfo.Length;
            Debug.WriteLine($"Changed: {fileInfo.LastWriteTime} {fileInfo.Length}");
            changes?.AddChangedItem(Item.Get(fi, Context.CurrentPath));
        }
    }

    Item[] MapItems()
        => [.. viewItems.Select(n =>
            n switch
            {
                ParentItem p => new Item(p.Name, n.GetIcon(Context.CurrentPath), ["", "", ""]),
                DirectoryItem d => Item.Get(d),
                FileItem f => Item.Get(f, Context.CurrentPath),
                _ => throw new Exception("Unknown ItemBase")
            })];

    readonly FileSystemWatcher watcher = new();

    ItemBase[] items = null!;
    ItemBase[] viewItems = null!;

    int sortIndex;
    bool sortDescending = false;
    bool sortSubcolumn = false;
    ExtendedFileInfos? extendedFileInfos;
    FileChanges? changes;

    #region IDispose

    protected override void Dispose(bool disposing)
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

    bool disposedValue;

    #endregion
}

