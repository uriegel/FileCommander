using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using CsTools;

namespace FileCommander.Controllers;

class MetaFileData
{
    public MetaFileData()
    {
        for (int i = 0; i < 4; i++)
            _ = MetadataWorker(metadataCts.Token);
    }

    public void QueueMetadata(ItemBase item, string path)
    {
        if (!metadataPending.TryAdd(path, 0))
            return;
        metadataQueue.Writer.TryWrite(path);
    }

    async Task MetadataWorker(CancellationToken cancellationToken)
    {
        await foreach (var path in metadataQueue.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                await ProcessMetadata(path, cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Metadata error for {path}: {ex}");
            }
            finally
            {
                metadataPending.TryRemove(path, out _);
            }
        }
    }

    async Task ProcessMetadata(string path, CancellationToken cancellationToken)
    {
        if (!await WaitUntilStable(path, cancellationToken))
            return;
        if (!File.Exists(path))
            return;
        var extension = Path.GetExtension(path);

        if (!extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            return;

        var exifDate = await Task.Run(
            () => ExifReader.GetExifData(path),
            cancellationToken);

        if (exifDate == null)
            return;

        Debug.WriteLine($"Aufgelöst: {path} {exifDate.DateTime}");
        //var item = items.FirstOrDefault(
        //    n => n.Name.Equals(
        //        Path.GetFileName(path),
        //        StringComparison.OrdinalIgnoreCase));

        //if (item is not FileItem fileItem)
        //    return;

        //fileItem.ExifDate = exifDate.Value;

        MainWindow.RunOnUI(() =>
        {
            // Notify/update the corresponding row here.
        });
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

    readonly Channel<string> metadataQueue = Channel.CreateUnbounded<string>();
    readonly CancellationTokenSource metadataCts = new();
    readonly ConcurrentDictionary<string, byte> metadataPending = new(StringComparer.OrdinalIgnoreCase);

}
