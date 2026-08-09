using FileCommander.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileCommander.Controllers;

class FileChanges
{
    public async Task AddChangedItemAsync(Item item)
    {
        await locker.WaitAsync();
        try
        {
            changedItems[item.Text] = item;
        }
        finally
        {
            locker.Release();
        }
    }

    public async Task<Item[]> GetItemsAsync()
    {
        await locker.WaitAsync();
        try
        {
            var items = changedItems.Values.ToArray();
            changedItems = [];
            return items;
        }
        finally
        {
            locker.Release();
        }
    }

    readonly SemaphoreSlim locker = new(1, 1);
    Dictionary<string, Item> changedItems = [];
}
