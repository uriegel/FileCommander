using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

using Windows.Devices.Bluetooth.Advertisement;

namespace FileCommander.Controls;

public class ItemGrid : Grid
{
    public Type Type { get; set; } = typeof(ItemGrid);

    public ItemGrid() => Loaded += OnLoaded;

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        var explorer = FindAncestor<ListView>(this);
        Console.WriteLine("Ein Element");
    }

    static T? FindAncestor<T>(DependencyObject d)
    where T : DependencyObject
    {
        while (d != null)
        {
            d = VisualTreeHelper.GetParent(d);

            if (d is T t)
                return t;
        }

        return null;
    }
}
