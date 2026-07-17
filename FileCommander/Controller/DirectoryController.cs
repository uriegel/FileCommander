using FileCommander.Data;
using FileCommander.DataStore;

using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FileCommander.Controller;

class DirectoryController
{
    public DirectoryController(Store store) => this.store = store;

    public async Task ChangePathAsync(string path)
    {
        //var folderToSelect = path.EndsWith("..") ? context.CurrentPath.SubstringAfterLast('/') : null;
        //        cancellation.Cancel();
        //        cancellation = new();
        var items = await Get(path);
        //var enableEvents = watcher.Path == "";
        //watcher.Path = context.CurrentPath;
        //if (enableEvents)
        //    watcher.EnableRaisingEvents = true;
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
        //context.CurrentPath = dirInfo.FullName;
        //Application.Settings.SetString($"path-{Id}", dirInfo.FullName);
        return [
            new ParentItem(),
            .. dirs,
            .. files
        ];
    }

    readonly Store store;
}
