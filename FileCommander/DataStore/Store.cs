using FileCommander.Controls;
using FileCommander.Data;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FileCommander.DataStore;

class Store
{
    public ObservableCollection<Item> Items { get; } = [];

    public void ChangeItems(IEnumerable<Item> items)
    {
        Items.Clear();
        foreach (var item in items)
            Items.Add(item);
    }
}
