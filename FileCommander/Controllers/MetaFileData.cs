using CsTools;

using FileCommander.Data;

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FileCommander.Controllers;

class MetaFileData
{
    public MetaFileData(Func<FileChanges?> getChanges)
    {
        for (int i = 0; i < 4; i++)
            _ = MetadataWorker(metadataCts.Token);
        this.getChanges = getChanges;
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
            !extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
            return;

        var exifData = await Task.Run(
            () => ExifReader.GetExifData(job.Path),
            cancellationToken);

        if (exifData == null)
            return;

        Debug.WriteLine($"Aufgelöst: {job.Path} {exifData.DateTime}");
        job.Item.ExifData = exifData;
        getChanges()?.AddChangedItem(Item.Get(job.Item, job.Path)); // Test
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
    readonly CancellationTokenSource metadataCts = new();
    readonly ConcurrentDictionary<string, byte> metadataPending = new(StringComparer.OrdinalIgnoreCase);
    readonly Func<FileChanges?> getChanges;

    record Job(string Path, FileItem Item);
}

