using CsTools;
using CsTools.Extensions;

using FileCommander.Contexts;
using FileCommander.Data;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileCommander.Controllers;

class ExtendedFileInfos : IDisposable
{
    public ExtendedFileInfos(FileChanges changes, string path, FolderContext context, IEnumerable<FileItem> items)
    {
        this.changes = changes;
        context.InfoText = "Erweiterte Infos werden ermittelt...";
        task = Task.Run(() => GetAsync(path, context, items));
        task.GetAwaiter().OnCompleted(() => context.InfoText = null);
    }

    async Task GetAsync(string path, FolderContext context, IEnumerable<FileItem> items)
    {
        var stoppwatch = Stopwatch.StartNew();
        var extendedItems = GetExtendedFileItems(items);
        if (extendedItems.Length > 0)
            await foreach (var extendedItem in extendedItems.ToAsyncEnumerable()) 
            {
                if (cancellation.IsCancellationRequested)
                    return;

                if (extendedItem.Exif)
                {
                    var info = ExifReader.GetExifData(path.AppendPath(extendedItem.Item.Name));
                    if (info != null)
                    {
                        extendedItem.Item.ExifData = info;
                        changes.AddChangedItem(Item.Get(extendedItem.Item, path));
                    }
                }
                if (extendedItem.Version)
                {
                    var info = FileVersionInfo.GetVersionInfo(path.AppendPath(extendedItem.Item.Name));
                    if (info != null)
                    {
                        extendedItem.Item.Version = info;
                        changes.AddChangedItem(Item.Get(extendedItem.Item, path));
                    }
                }
            }
        var timeNeeded = stoppwatch.Elapsed;
        Debug.WriteLine($"Get extended needed {timeNeeded.TotalMilliseconds} ms for {extendedItems.Length} items");
    }

    static ExtendedFileItem[] GetExtendedFileItems(IEnumerable<FileItem> items)
        => [.. items.Select(n => new ExtendedFileItem(IsVersion(n), IsExif(n), n))];


    static bool IsExif(FileItem item)
    {
        var ext = item.Name.GetFileExtension();
        return string.CompareOrdinal(ext, ".jpg") == 0
           || string.CompareOrdinal(ext, ".png") == 0;
    }

    static bool IsVersion(FileItem item)
    {
        var ext = item.Name.GetFileExtension();
        return string.CompareOrdinal(ext, ".exe") == 0
           || string.CompareOrdinal(ext, ".dll") == 0;
    }

    readonly FileChanges changes;
    readonly CancellationTokenSource cancellation = new();
    readonly Task task;

    #region IDisposable 

    public void Dispose()
    {
        // Ändere diesen Code nicht. Füge Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // Verwalteten Zustand (verwaltete Objekte) bereinigen
                cancellation.Cancel();
            }

            // Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
            // Große Felder auf NULL setzen
            disposedValue = true;
        }
    }

    // Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
    // ~ExtendedFileInfos()
    // {
    //     // Ändere diesen Code nicht. Füge Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
    //     Dispose(disposing: false);
    // }

    bool disposedValue;

    #endregion
}

record ExtendedFileItem(bool Version, bool Exif, FileItem Item);
