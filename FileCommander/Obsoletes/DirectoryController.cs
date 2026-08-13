//using ClrWinApi;

//using CsTools.Extensions;

//using FileCommander.Controls;

//using Microsoft.Web.WebView2.Core;

//using System;
//using System.Drawing;
//using System.Drawing.Imaging;
//using System.IO;
//using System.Linq;
//using System.Runtime.InteropServices;
//using System.Threading.Tasks;

//namespace FileCommander.Obsoletes;

//class DirectoryController : IDisposable
//{
//    public async Task ChangePathAsync(string path)
//    {

//        //MainContext.Instance.PropertyChanged -= OnPropertyChanged;
//        //MainContext.Instance.PropertyChanged += OnPropertyChanged;
//    }

//    void WatchChanged(object _, FileSystemEventArgs e)
//    {
//    //    var fileInfo = new FileInfo(context.CurrentPath.AppendPath(e.Name));
////        var item = store.Items.FirstOrDefault(n => n.Name == e.Name);
//        //if (item is DirectoryItem di)
//        //{
//        //    di.DateTime = fileInfo.LastWriteTime;
//        //}
//        //if (item is FileItem fi)
//        //{
//        //    fi.DateTime = fileInfo.LastWriteTime;
//        //    fi.Size = fileInfo.Length;
//        //}
//    }

//    void WatchRenamed(object _, RenamedEventArgs e)
//    {
//        //Console.WriteLine($"Renamed: {e.OldName} {e.Name}");
//        //int focused = model.Selected;
//        //var pos = model.GetItems<DirectoryItem>().TakeWhile(n => n.Name != e.OldName).Count();
//        //bool focusNew = pos == focused;

//        //var posToRemove = store.GetItems<DirectoryItem>().TakeWhile(n => n.Name != e.OldName).Count();
//        //if (pos != store.GetItemsCount())
//        //    store.Remove(posToRemove);

//        //var fileInfo = new FileInfo(context.CurrentPath.AppendPath(e.Name));
//        //if (!File.Exists(context.CurrentPath.AppendPath(e.Name)))
//        //    store.Splice(0, 0, [DirectoryItem.CreateFileItem(fileInfo)]);
//        //else
//        //{
//        //    var item = model.GetItems<DirectoryItem>().FirstOrDefault(n => n.Name == e.Name);
//        //    item?.DateTime = fileInfo.LastWriteTime;
//        //    item?.Size = fileInfo.Length;
//        //}
//        //view.CountsChanged(GetDirectoryCount(), GetFileCount());

//        //if (focusNew)
//        //{
//        //    var newPos = model
//        //        .GetItems<DirectoryItem>()
//        //        .Select((n, i) => new DirItemPos(Item: n, Pos: i))
//        //        .FirstOrDefault(n => n.Item.Name == e.Name)?.Pos;
//        //    if (newPos.HasValue)
//        //        SetSelection(newPos.Value);
//        //}
//    }

