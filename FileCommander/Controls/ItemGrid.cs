using FileCommander.Data;

using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

using System;
using System.ComponentModel;
using System.Diagnostics;

namespace FileCommander.Controls;

public class ItemGrid : Grid
{
    public static readonly DependencyProperty IsCurrentProperty = DependencyProperty.Register("IsCurrent", typeof(bool),
        typeof(ItemGrid), new PropertyMetadata(false, IsCurrentChanged));
    public bool IsCurrent
    {
        get { return (bool)GetValue(IsCurrentProperty); }
        set { SetValue(IsCurrentProperty, value); }
    }
    static void IsCurrentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ItemGrid grid)
            grid.IsCurrentChanged();
    }

    void IsCurrentChanged()
    {
        BorderBrush = IsCurrent && Context?.IsFocused == true
            ? Rot
            : IsCurrent && Context?.IsFocused != true
            ? DurchsichtigCurrent
            : Durchsichtig;
    }

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
        BorderBrush = Durchsichtig;
        BorderThickness = new Thickness(1);
        Background = Durchsichtig;
        DataContextChanged += (_, e) =>
        {
            //SetBinding(IsCurrentProperty, new Binding()
            //{
            //    Converter = new Konverter(this),
            //    Source = Context,
            //    Path = new PropertyPath(nameof(Context.SelectedItem)),
            //});
            //SetBinding(BorderBrushProperty, new Binding()
            //{
            //    Converter = new Konverter2(this),
            //    Source = Context,
            //    Path = new PropertyPath(nameof(Context.SelectedItem)),
            //});
        };

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
        //SetBinding(IsCurrentProperty, new Binding()
        //{
        //    Converter = new Konverter(this),
        //    Source = Context,
        //    Path = new PropertyPath(nameof(Context.SelectedItem)),
        //});
        //SetBinding(BorderBrushProperty, new Binding()
        //{
        //    Converter = new Konverter2(this),
        //    Source = Context,
        //    Path = new PropertyPath(nameof(Context.SelectedItem)),
        //});
    }

    public void PropertyChanged(object? s, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Context.ColumnWidths))
        {
            int i = 0;
            foreach (var def in Context!.ColumnWidths)
                ColumnDefinitions[i++].Width = def;
        }
        else if (e.PropertyName == nameof(Context.IsFocused))
        {
            IsCurrentChanged();
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

class Konverter2(ItemGrid grid) : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => (grid.DataContext as Item)?.Equals(value as Item) == true
            ? ItemGrid.Rot
            : ItemGrid.Durchsichtig;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
class Konverter(ItemGrid grid) : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => (grid.DataContext as Item)?.Equals(value as Item) == true;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
