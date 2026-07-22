using FileCommander.Data;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using WinUITools.ItemsRepeaterExtensions;

namespace FileCommander.DataStore;

class Store
{
    public ObservableCollection<ColumnViewItem> Items { get; } = [];

    public void ChangeItems(IEnumerable<Item> items)
    {
        Items.Clear();
        foreach (var item in items)
            Items.Add(item);
        indexes = Items.Select((n, i) => (n, i)).ToDictionary();
    }

    public int GetIndex(Item? item)
    {
        return item != null
        ? indexes.TryGetValue(item, out var index)
        ? index
        : -1
        : -1;
    }

    public int GetCount() => indexes.Count;

    Dictionary<ColumnViewItem, int> indexes = [];
}
