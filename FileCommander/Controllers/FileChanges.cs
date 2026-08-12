using System;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

using FileCommander.Data;

namespace FileCommander.Controllers;

class FileChanges
{
    public async Task AddChangedItemAsync(Item item)
        => await changedItems.Writer.WriteAsync(item); // TODO Cancellation

    public async Task<Item[]> GetItemsAsync()
    {
        var items = new List<Item>();

        var now = DateTime.Now;        
        // First, consume everything that is already available.
        while (changedItems.Reader.TryRead(out var item) && now + TimeSpan.FromMilliseconds(10) > DateTime.Now)
            items.Add(item);

        // Nothing was available -> wait for the next item.
        if (items.Count == 0)
            items.Add(await changedItems.Reader.ReadAsync());

        return [.. items];
    }

    readonly Channel<Item> changedItems = Channel.CreateUnbounded<Item>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = true
    });
}
