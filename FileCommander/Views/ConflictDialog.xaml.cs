using FileCommander.Controllers;

using Microsoft.UI.Xaml;

using System.Collections.Generic;
using System.Collections.ObjectModel;

using Windows.Graphics;
using Windows.System;

namespace FileCommander.Views;

// TODO await Dialog with response
// TODO Buttons same size
// TODO Default Button 

public sealed partial class ConflictDialog : Window
{
    public bool FromRight { get; }

    ObservableCollection<ConflictItem> Items { get; } = [];

    public ConflictDialog()
    {
        InitializeComponent();
        AppWindow.Resize(new SizeInt32(1000, 800));
        ListView.ItemsSource = Items;
    }

    internal ConflictDialog(IEnumerable<ConflictItem> conflicts, string description, bool fromRight) 
        : this()
    {
        FromRight = fromRight;
        Description.Text = description;

        foreach (var item in conflicts)
        {
            Items.Add(item);
            Items.Add(item);
            Items.Add(item);
            Items.Add(item);
            Items.Add(item);
            Items.Add(item);
        }
    }

    void Grid_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
            Close();
    }
}

