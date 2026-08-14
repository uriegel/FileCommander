using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using FileCommander.Data;

namespace FileCommander.Controllers;

class FileChanges : IDisposable
{
    public async Task AddChangedItemAsync(Item item)
        => await changedItems.Writer.WriteAsync(item, cancellation.Token);

    public async Task<FileChangesResult?> GetItemsAsync()
    {
        var items = new List<Item>();

        var now = DateTime.Now;        
        // First, consume everything that is already available.
        while (changedItems.Reader.TryRead(out var item) && now + TimeSpan.FromMilliseconds(10) > DateTime.Now)
            items.Add(item);


        // Show if Full change

        // Nothing was available -> wait for the next item.
        try
        {
            if (items.Count == 0)
                items.Add(await changedItems.Reader.ReadAsync(cancellation.Token));
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        return items.Count > 0 ? new([.. items], false) : null;
    }

    readonly Channel<Item> changedItems = Channel.CreateUnbounded<Item>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = true
    });
    
    readonly CancellationTokenSource cancellation = new();

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

    // // Finalizer nur überschreiben, wenn "Dispose(bool disposing)" Code für die Freigabe nicht verwalteter Ressourcen enthält
    // ~FileChanges()
    // {
    //     // Ändere diesen Code nicht. Füge Bereinigungscode in der Methode "Dispose(bool disposing)" ein.
    //     Dispose(disposing: false);
    // }

    bool disposedValue;

    #endregion
}

record FileChangesResult(Item[] Items, bool Complete = false);