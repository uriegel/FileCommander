using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

using Windows.Devices.Bluetooth.Advertisement;

namespace FileCommander.Controls;

public class ItemGrid : Grid
{
    public Type Type { get; set; } = typeof(ItemGrid);

    public ItemGrid() => Loaded += OnLoaded;

    public void Prepare()
        => context?.PropertyChanged += PropertyChanged;

    public void Reset()
        => context?.PropertyChanged -= PropertyChanged;

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        actives++;
        Debug.WriteLine($"Geladen: {actives}");
        var explorer = FindAncestor<ColumnView>(this);
        context = explorer?.DataContext as Context;
        context?.PropertyChanged += PropertyChanged;
        int i = 0;
        foreach (var def in context!.ColumnWidths)
            ColumnDefinitions[i++].Width = def;
    }

    public void PropertyChanged(object? s, PropertyChangedEventArgs e) 
    {
        if (e.PropertyName == nameof(Context.ColumnWidths))
        {
            int i = 0;
            foreach (var def in context!.ColumnWidths)
                ColumnDefinitions[i++].Width = def;
        }
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

    static int actives;

    Context? context;
}