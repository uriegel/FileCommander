using CsTools;
using CsTools.Extensions;

using FileCommander.Data;

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FileCommander.Controllers;

class MetaFileData : IDisposable
{
    public MetaFileData(FileChanges changes)
    {
        for (int i = 0; i < 4; i++)
            _ = MetadataWorker(cts.Token);
        this.changes = changes;
    }

    public void QueueMetadata(FileItem item, string path)
    {
        if (!metadataPending.TryAdd(path, 0))
            return;
        metadataQueue.Writer.TryWrite(new(path, item));
    }

    async Task MetadataWorker(CancellationToken cancellationToken)
    {
        await foreach (var job in metadataQueue.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await ProcessMetadata(job, cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Metadata error for {job.Path}: {ex}");
            }
            finally
            {
                metadataPending.TryRemove(job.Path, out _);
            }
        }
    }

    async Task ProcessMetadata(Job job, CancellationToken cancellationToken)
    {
        if (!await WaitUntilStable(job.Path, cancellationToken))
            return;
        if (!File.Exists(job.Path))
            return;
        var extension = Path.GetExtension(job.Path);

        if (!extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".dll", StringComparison.OrdinalIgnoreCase))
            return;

        var exifData = await Task.Run(() => ExifReader.GetExifData(job.Path), cancellationToken);
        if (exifData != null)
        {
            job.Item.ExifData = exifData;
            changes.AddChangedItem(Item.Get(job.Item, job.Path.SubstringAfterLast('\\')));
        }
        var version = await Task.Run(() => FileVersionInfo.GetVersionInfo(job.Path), cancellationToken);
        if (version != null)
        {
            job.Item.Version = version;
            changes.AddChangedItem(Item.Get(job.Item, job.Path.SubstringAfterLast('\\')));
        }
    }

    static async Task<bool> WaitUntilStable(string path, CancellationToken cancellationToken)
    {
        const int interval = 150;

        long lastSize = -1;
        DateTime lastWrite = default;

        for (int attempt = 0; attempt < 40; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var info = new FileInfo(path);

                if (!info.Exists)
                {
                    await Task.Delay(interval, cancellationToken);
                    continue;
                }

                var size = info.Length;
                var write = info.LastWriteTimeUtc;

                if (size == lastSize && write == lastWrite)
                    return true;

                lastSize = size;
                lastWrite = write;
            }
            catch (IOException)
            {
                // File is currently unavailable.
            }
            catch (UnauthorizedAccessException)
            {
            }

            await Task.Delay(interval, cancellationToken);
        }

        return false;
    }

    readonly Channel<Job> metadataQueue = Channel.CreateUnbounded<Job>();
    readonly ConcurrentDictionary<string, byte> metadataPending = new(StringComparer.OrdinalIgnoreCase);
    readonly FileChanges changes;
    readonly CancellationTokenSource cts = new();

    record Job(string Path, FileItem Item);

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
                // Verwalteten Zustand (verwaltete Objekte) bereinigen
                cts.Cancel();

            // Nicht verwaltete Ressourcen (nicht verwaltete Objekte) freigeben und Finalizer überschreiben
            // Große Felder auf NULL setzen
            disposedValue = true;
        }
    }

    // Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
    // ~MetaFileData()
    // {
    //     // Ändere diesen Code nicht. Füge Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
    //     Dispose(disposing: false);
    // }

    bool disposedValue;

    #endregion
}

