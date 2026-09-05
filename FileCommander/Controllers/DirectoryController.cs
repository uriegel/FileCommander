using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ClrWinApi;

using CsTools.Extensions;

using FileCommander.Contexts;
using FileCommander.Controls;
using FileCommander.Data;
using FileCommander.Views;


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

    public override Task<(Item[] Items, int oldPos, int dirCount, int fileCount)> GetItemsAsync(
        string path, bool controllerChanged, bool fromHistory = false)
        => NetworkShare.ExecuteAsync(path, () => RunGetItems(path, controllerChanged, fromHistory));
    
    (Item[] Items, int oldPos, int dirCount, int fileCount) RunGetItems(
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
        items = ((ItemBase[])[new ParentItem(), .. dirItems, .. fileItems]).ToDictionary(n => n.Name);
        changes?.Dispose();
        changes = new();
        extendedFileInfos?.Dispose();
        extendedFileInfos = new(changes, Context.CurrentPath, Context, items.Values.SelectFilterNull(n => n as FileItem));
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
        {
            Execute(pos);
            return true;
        }
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

    public override async Task<(Item[] Items, int newPos, int dirs, int files)> ReloadAsync(int pos)
    {
        var recentItem = viewItems[pos].Name;
        var (items, _, dirs, files) = await GetItemsAsync(Context.CurrentPath, false);
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

    public override async void CreateFolder() 
    {
        var newName = await Dialog.ShowAsync(MainWindow.Content,
            "Ordner anlegen",
            dialog => (dialog.Content as CreateFolderDialog)?.FolderName ?? "",
            new CreateFolderDialog()
            {
                FolderName = Context.SelectedPath.EndsWith("..") == false ? Context.SelectedPath.SubstringAfterLast('\\') : ""
            });
        if (newName != null)
        { 
            try
            {
                System.IO.Directory.CreateDirectory(Context.CurrentPath.AppendPath(newName));
            }
            catch (UnauthorizedAccessException)
            {
                var temp = System.IO.Path.GetTempFileName();
                File.Delete(temp);
                System.IO.Directory.CreateDirectory(temp);
                var sourcePath = temp.AppendPath(newName);
                System.IO.Directory.CreateDirectory(sourcePath);
                var res = Api.SHFileOperation(new ShFileOPStruct
                {
                    Func = FileFuncFlags.MOVE,
                    From = $"{sourcePath}\U00000000\U00000000",
                    To = $"{Context.CurrentPath}\U00000000\U00000000",
                });
                System.IO.Directory.Delete(temp, true);
                ProcessResult(res);
            }
            catch (System.IO.IOException ioe)
            {
                MainWindow.ShowError(ioe.Message);
            }
            catch (Exception e)
            {
                MainWindow.ShowError(e.Message);
            }
        }
    }

    public override async Task<bool> DeleteItems(int[] items) 
    {
        var itemsToDelete = items
            .Select(n => (viewItems[n] as ItemBase))
            .ToArray();
        var (dirs, files) = GetDirAndFileCount(itemsToDelete);
        var pathsToDelete = itemsToDelete.Select(n => Context.CurrentPath.AppendPath(n.Name)).ToArray();
        var dirAndFileText = GetDirAndFileText(dirs, files);

        if (await Dialog.ShowAsync(MainWindow.Content,
            "Dateien löschen",
            textContent: $"Möchtest du {dirAndFileText} löschen?"))
        {
            var res = await Task.Run(() => Api.SHFileOperation(new ShFileOPStruct
            {
                Func = FileFuncFlags.DELETE,
                From = string.Join("\U00000000", pathsToDelete) + "\U00000000\U00000000",
                Flags = FileOpFlags.ALLOWUNDO
            }));
            return ProcessResult(res);
        }
        else
            return false;
    }

    public override async Task<bool> Copy(CopyItems items, VirtualTable otherSide, bool fromRight) 
    { 
        if (otherSide.Controller is not DirectoryController)
            return false;

        var itemsToCopy = items.Items
            .Select(n => (viewItems[n]))
            .ToArray();
        var (dirs, files) = GetDirAndFileCount(itemsToCopy);
        var titleAction = items.Move ? "verschieben" : "kopieren";
        var title = (dirs, files) switch
        {
            (0, 1) => $"Datei {titleAction}",
            (1, 0) => $"Verzeichnis {titleAction}",
            (_, 0) => $"Verzeichnisse {titleAction}",
            (0, _) => $"Dateien {titleAction}",
            _ => $"Verzeichnisse und Dateien {titleAction}"
        };
        var dirAndFileText = (dirs, files) switch
        {
            (0, 1) => "die Datei",
            (1, 0) => "das Verzeichnis",
            (_, 0) => "die Verzeichnisse",
            (0, _) => "die Dateien",
            _ => "die Verzeichnisse und Dateien"
        };

        var targets = otherSide.Controller.GetViewItems();
        var conflicts = CopyTools.GetConflicts(itemsToCopy, targets, Context.CurrentPath).ToArray();
        bool noConfirmation = false;
        if (conflicts.Length > 0)
        {
            var conflictResult = await ConflictDialog.ShowAsync([.. conflicts], titleAction.CapitalizeFirst(), fromRight);
            if (conflictResult == ConflictDialogResult.Canceled)
                return false;
            if (conflictResult == ConflictDialogResult.DoNotOverwrite)
                itemsToCopy = ExceptConflicts(itemsToCopy, conflicts);
            else 
                noConfirmation = true;
        } 
        else
        {
            if (!await Dialog.ShowAsync(MainWindow.Content, title, new CopyDialog() 
            {
                Description = $"Möchtest du {dirAndFileText} {titleAction}?",
                FromRight = fromRight
            }))
                return false;
        }

        var path = Context.CurrentPath;
        var otherPath = otherSide.Context.CurrentPath;
        var res = await Task.Run(() => Api.SHFileOperation(new ShFileOPStruct
        {
            Func = items.Move ? FileFuncFlags.MOVE : FileFuncFlags.COPY,
            From = string.Join("\U00000000", itemsToCopy.Select(n => Context.CurrentPath.AppendPath(n.Name))) + "\U00000000\U00000000",
            To = string.Join("\U00000000", itemsToCopy.Select(n => otherSide.Context.CurrentPath.AppendPath(n.Name))) + "\U00000000\U00000000",
            Flags = (noConfirmation ? FileOpFlags.NOCONFIRMATION : FileOpFlags.NOCONFIRMMKDIR) | FileOpFlags.NOCONFIRMMKDIR | FileOpFlags.MULTIDESTFILES,
        }));
        return ProcessResult(res);

        ItemBase[] ExceptConflicts(ItemBase[] items, ConflictItem[] conflicts)
        {
            var conflictNames = conflicts
                .Select(c => c.Name)
                .ToHashSet();
            return itemsToCopy = [
                .. items
                    .Where(n => !conflictNames.Contains(n.Name))
            ];
        }
    }

    public override async Task<bool> Rename(int pos, bool asCopy)
    {
        try
        {
            var item = viewItems[pos];
            var newName = await Dialog.ShowAsync(MainWindow.Content, asCopy ? "Kopie anlegen" : "Umbenennen",
                d => (d.Content as RenameDialog)?.FileName ?? "",
                new RenameDialog()
                {
                    Description = $"Möchtest du {(item is FileItem ? "die Datei" : "das Verzeichnis")} umbenennen?",
                    FileName = item.Name
                });
            if (newName != null)
            {
                var res = Api.SHFileOperation(new ShFileOPStruct
                {
                    Func = asCopy == true ? FileFuncFlags.COPY : FileFuncFlags.RENAME,
                    From = Context.CurrentPath.AppendPath(item.Name) + "\U00000000\U00000000",
                    To = Context.CurrentPath.AppendPath(newName) + "\U00000000\U00000000",
                    Flags = FileOpFlags.NOCONFIRMATION | FileOpFlags.ALLOWUNDO
                });
                return ProcessResult(res);
            }
            return false;
        }
        catch
        {
            MainWindow.ShowError($"Unbekannter Fehler aufgetreten");
            return false; 
        }
    }

    public override void Execute(int pos)
    {
        using var proc = new Process()
        {
            StartInfo = new ProcessStartInfo(Context.CurrentPath.AppendPath(viewItems[pos].Name))
            {
                UseShellExecute = true,
            },
        };
        proc.Start();
    }

    public override void OnEnter(int pos, bool openWith)
    {
        var info = new ShellExecuteInfo();
        info.Size = Marshal.SizeOf(info);
        info.Verb = openWith ? "openas" : "properties";
        info.File = Context.CurrentPath.AppendPath(viewItems[pos].Name);
        info.Show = ShowWindowFlag.Show;
        info.Mask = ShellExecuteFlag.InvokeIDList;
        Api.ShellExecuteEx(ref info);
    }

    public override ItemBase[] GetViewItems() => viewItems;

    static bool ProcessResult(int res)
    {
        if (res == 0)
            return true;
        else if (res == 2)
            MainWindow.ShowError($"Nicht gefunden");
        else if (res == 0x78)
            MainWindow.ShowError($"Zugriff verweigert");
        //else if (res == 1223)
        //    MainWindow.ShowError($"Vorgang abgebrochen");
        return false;
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

    (int Dirs, int Files) GetDirAndFileCount(ItemBase[] items)
        => (items.Count(n => n is DirectoryItem), items.Count(n => n is FileItem));

    static string GetDirAndFileText(int dirs, int files)
        => dirs == 1 && files == 0
            ? "das Verzeichnis"
            : dirs == 0 && files == 1
            ? "die Datei"
            : dirs > 0 && files == 0
            ? "die Verzeichnisse"
            : dirs == 0 && files > 0
            ? "die Dateien"
            : "die Dateien und Verzeichnisse";

    (ItemBase[], int) MapViewItems(string? fromPath)
    {
        var filtered = items.Values
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
            items.TryAdd(newItem.Name, newItem);
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
            if (MainContext.Instance.ShowHidden || !item.Hidden)
            {
                changes?.AddCreateItem(item, pos, selpos);

                if (newItem is FileItem fileItem)
                    changes?.QueueMetadata(fileItem, e.FullPath);
            }
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
            var isFile = e.Name != null && items.TryGetValue(e.Name, out var val) && val is FileItem;
            if (e.Name != null)
                items.Remove(e.Name);
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
            ItemBase? oldItem = null;
            if ((e.OldName == null || items.TryGetValue(e.OldName, out oldItem)) == false)
            {
                WatchCreated(this, new FileSystemEventArgs(WatcherChangeTypes.Created, e.FullPath, e.Name));
                return;
            }
            if (e.OldName == null || oldItem == null)
                return;
            var oldPos = Array.IndexOf(viewItems, oldItem);
            var newItem = oldItem with { Name = e.Name ?? "" };

            items.TryAdd(newItem.Name, newItem);
            if (e.OldName != null)
                items.Remove(e.OldName);

            var selectedItem = Context.SelectedPath.SubstringAfterLast('\\');

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

        if ((e.Name != null && items.TryGetValue(e.Name, out var item)) == false)
            return;
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
            try
            {
                fi.Size = fileInfo.Length;
                Debug.WriteLine($"Changed: {fileInfo.LastWriteTime} {fileInfo.Length}");
                changes?.AddChangedItem(Item.Get(fi, Context.CurrentPath));

                changes?.QueueMetadata(fi, e.FullPath);
            }
            catch (System.IO.FileNotFoundException fnfe) 
            {
                Debug.WriteLine(fnfe);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }

    readonly FileSystemWatcher watcher = new();
    
    Dictionary<string, ItemBase> items = null!;
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

static class DirectoryControllerExtensions
{
    public static string CapitalizeFirst(this string text)
    {
        return string.IsNullOrEmpty(text)
            ? text
            : char.ToUpper(text[0]) + text[1..];
    }
}