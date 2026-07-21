using FileCommander.Data;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using System;
using System.ComponentModel;
using System.Diagnostics;

namespace FileCommander.Controls;

public class ItemGrid : Grid
{
    public Type Type { get; set; } = typeof(ItemGrid);

    public ItemGrid()
    {
        Loaded += OnLoaded;
    }

    public void Prepare()
    {
        Context?.PropertyChanged += PropertyChanged;
        int i = 0;
        if (Context != null)
            foreach (var def in Context!.ColumnWidths)
                ColumnDefinitions[i++].Width = def;
    }

    public void Reset()
    {
        Context?.PropertyChanged -= PropertyChanged;
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        actives++;
        BorderBrush = Durchsichtig;
        BorderThickness = new Thickness(1);
        Background ??= Durchsichtig;
        Debug.WriteLine($"Geladen: {actives}");
        var explorer = FindAncestor<ColumnView>(this);
        Context = explorer?.DataContext as Context;
        Context?.PropertyChanged += PropertyChanged;
        Context?.ItemsHeight = ActualHeight + 2;


        IsTabStop = true;

        GotFocus += (_, _) =>
        {
            BorderBrush = new SolidColorBrush(Colors.Red);
            BorderThickness = new Thickness(1);
        };

        LostFocus += (_, _) =>
        {
            BorderBrush = new SolidColorBrush(Colors.Transparent);
            BorderThickness = new Thickness(1);
        };

        PointerPressed += (_, _) =>
        {
            Context?.SelectedItem = DataContext as Item;
        };

        int i = 0;
        foreach (var def in Context!.ColumnWidths)
            ColumnDefinitions[i++].Width = def;
    }

    public void PropertyChanged(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Context.ColumnWidths))
        {
            int i = 0;
            foreach (var def in Context!.ColumnWidths)
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

    internal Context? Context { get; private set; }



    public static SolidColorBrush Rot = new(Colors.Red);
    public static SolidColorBrush Grau = new(Colors.Gray);
    public static SolidColorBrush Durchsichtig = new(Colors.Transparent);
    public static SolidColorBrush DurchsichtigCurrent = new(Colors.LightGray);
}

