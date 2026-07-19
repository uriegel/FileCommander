using FileCommander.Data;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
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
    }

    public void Reset()
    {
        Context?.PropertyChanged -= PropertyChanged;
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        actives++;
        BorderBrush = Rot;
        BorderThickness = new Thickness(1);
        Background = Durchsichtig;
        DataContextChanged += (_, e) =>
        {
            SetBinding(BorderBrushProperty, new Binding()
            {
                Converter = new Konverter2(this),
                Source = Context,
                Path = new PropertyPath(nameof(Context.SelectedItem)),
            });
        };
        IsTabStop = true;
        Debug.WriteLine($"Geladen: {actives}");
        var explorer = FindAncestor<ColumnView>(this);
        Context = explorer?.DataContext as Context;
        Context?.PropertyChanged += PropertyChanged;
        Context?.ItemsHeight = ActualHeight + 2;


        PointerPressed += (_, _) =>
        {
            Context?.SelectedItem = DataContext as Item;
        };

        int i = 0;
        foreach (var def in Context!.ColumnWidths)
            ColumnDefinitions[i++].Width = def;
        SetBinding(BorderBrushProperty, new Binding()
        {
            Converter = new Konverter2(this),
            Source = Context,
            Path = new PropertyPath(nameof(Context.SelectedItem)),
        });
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



    public static SolidColorBrush Rot = new SolidColorBrush(Colors.Red);
    public static SolidColorBrush Durchsichtig = new SolidColorBrush(Colors.Transparent);
}

class Konverter2(ItemGrid grid) : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => (grid.DataContext as Item)?.Equals(value as Item) == true
            ? ItemGrid.Rot
            : ItemGrid.Durchsichtig;


    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
