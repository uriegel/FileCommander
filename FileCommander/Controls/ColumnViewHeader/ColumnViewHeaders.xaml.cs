using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Windows.Devices.Input;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FileCommander.Controls.ColumnViewHeader;

public sealed partial class ColumnViewHeaders : UserControl
{
    #region Routed Events

    //public static readonly RoutedEvent SortChangedEvent =
    //    EventManager.RegisterRoutedEvent("SortChanged", RoutingStrategy.Bubble, typeof(SortChangedEventHandler),
    //        typeof(ColumnViewHeaders));

    //public event SortChangedEventHandler SortChanged
    //{
    //    add { AddHandler(SortChangedEvent, value); }
    //    remove { RemoveHandler(SortChangedEvent, value); }
    //}

    #endregion 

    #region Attached Properties

    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.RegisterAttached(
        "Columns", typeof(Context), typeof(ColumnViewHeaders), new PropertyMetadata(null, ColumnsChanged));

    public static Context GetColumns(DependencyObject obj) => (Context)obj.GetValue(ColumnsProperty);

    public static void SetColumns(DependencyObject obj, Context value) => obj.SetValue(ColumnsProperty, value);

    public static void ColumnsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
    {
        if (obj is not Grid)
            return;

        var ctx = (Context)e.NewValue;
        var grid = (Grid)obj;
        grid.ColumnDefinitions.Clear();

        var idx = 0;
        foreach (var col in ctx.StarLength.Cast<double>())
        {
            var coldef = new ColumnDefinition();
            var binding = new Binding
            {
                Source = e.NewValue,
                Path = new PropertyPath($"StarLength[{idx++}]"),
                Converter = new HeaderGridLengthConverter()
            };
            BindingOperations.SetBinding(coldef, ColumnDefinition.WidthProperty, binding);
            grid.ColumnDefinitions.Add(coldef);
        }
    }

    #endregion 

    public ColumnViewContext? ColumnViewContext
    {
        get => field;
        set
        {
            field = value;
            if (ColumnViewContext != null && DataContext is Context ctx)
                ColumnViewContext.ColumnWidths = [.. ctx.StarLength.Select(n => new GridLength(n, GridUnitType.Star))];
        }
    }

    public HeaderItem[] HeaderItems
    {
        get => field;
        set
        {
            field = value;

            var idx = 0;
            foreach (var item in field)
                item.Index = idx++;
            if (DataContext is Context ctx)
                ctx.StarLength = [.. field.Select(n => 1)];
            Headers.ItemsSource = field;
            //Headers.MouseMove += OnMouseMove;
            // border.SetResourceReference(Border.BorderBrushProperty, "MyBorderBrush");
        }
    } = [];

    public ColumnViewHeaders()
    {
        InitializeComponent();
        DataContext = new Context { StarLength = [] };
    }

    double start;
    int startIndex;
    double[] stars = [];
    bool first;
    double startPos;
    double[] initialStars = [];

    void OnMouseMove(object? sender, MouseEventArgs e)
    {
        //var dO = e.OriginalSource as DependencyObject;
        //var border = dO?.FindAncestorOrSelf<Border>();
        //if (border != null
        //        && border.DataContext is HeaderItem item
        //        && DataContext is Context ctx)
        //{
        //    if (!dragging)
        //    {
        //        var pos = e.GetPosition(border);
        //        if (pos.X < 10 && item.Index != 0)
        //        {
        //            border.Cursor = Cursors.ScrollWE;
        //            if (e.LeftButton == MouseButtonState.Pressed && !dragging)
        //            {
        //                InitializeDrag();
        //                first = false;
        //                startIndex = item.Index - 1;
        //                start = pos.X;
        //                startPos = border.TransformToAncestor(Headers).Transform(new Point(0, 0)).X;
        //            }
        //        }
        //        else if (pos.X > border.ActualWidth - 10 && item.Index < ctx.StarLength.Length - 1)
        //        {
        //            border.Cursor = Cursors.ScrollWE;
        //            if (e.LeftButton == MouseButtonState.Pressed && !dragging)
        //            {
        //                InitializeDrag();
        //                first = true;
        //                startIndex = item.Index;
        //                start = pos.X;
        //                startPos = -1;
        //            }
        //        }
        //        else
        //            border.Cursor = Cursors.Arrow;
        //    }
        //    else
        //    {
        //        border.Cursor = Cursors.ScrollWE;

        //        var newStars = new double[ctx.StarLength.Length];
        //        if (first)
        //            stars.CopyTo(newStars);
        //        else
        //            ctx.StarLength.CopyTo(newStars);
        //        var diff = startPos == -1 ? e.GetPosition(border).X - start : e.GetPosition(Headers).X - startPos;
        //        newStars[startIndex] = initialStars[startIndex] + diff;
        //        newStars[startIndex + 1] = initialStars[startIndex + 1] - diff;
        //        if (newStars[startIndex] > 10 && newStars[startIndex + 1] > 10 && ColumnViewContext != null)
        //        {
        //            ctx.StarLength = newStars;
        //            ColumnViewContext.ColumnWidths = [.. ctx.StarLength.Select(n => new GridLength(n, GridUnitType.Star))];
        //        }
        //    }

        //    void InitializeDrag()
        //    {
        //        var grid = dO?.FindAncestorOrSelf<Grid>("ColumnViewHeaderGrid");
        //        var cols = grid?.Children.Cast<ContentPresenter>()?.ToArray();
        //        border.CaptureMouse();
        //        border.MouseLeftButtonUp += OnMouseUp;
        //        stars = new double[ctx.StarLength.Length];
        //        initialStars = new double[ctx.StarLength.Length];
        //        ctx.StarLength.CopyTo(stars);
        //        for (var i = 0; i < stars.Length; i++)
        //            stars[i] = cols?[i].ActualWidth ?? 0;
        //        ctx.StarLength = stars;
        //        ctx.StarLength.CopyTo(initialStars);
        //        dragging = true;
        //    }

        //    void OnMouseUp(object? sender, MouseEventArgs e)
        //    {
        //        if (sender is Border border && DataContext is Context ctx)
        //        {
        //            border.MouseLeftButtonUp -= OnMouseUp;
        //            border.ReleaseMouseCapture();
        //            dragging = false;
        //            border.Cursor = Cursors.Arrow;
        //        }
        //    }
        //}
    }

    //void Border_MouseDown(object sender, MouseButtonEventArgs e)
    //{
    //    if (sender is DependencyObject dO)
    //    {
    //        var border = dO.FindAncestorOrSelf<Border>();
    //        var index = (int)dO.GetValue(Grid.ColumnProperty);
    //        if (HeaderItems[index].SortType == SortType.Disabled || border?.Cursor == Cursors.ScrollWE)
    //            return;
    //        bool desc = HeaderItems[index].SortType == SortType.Ascending;
    //        foreach (var item in HeaderItems)
    //        {
    //            if (item.SortType != SortType.Disabled)
    //                item.SortType = SortType.None;
    //            item.SortType = SortType.None;
    //        }
    //        HeaderItems[index].SortType = desc ? SortType.Descending : SortType.Ascending;
    //        RaiseEvent(new SortChangedEventArgs(index, desc)
    //        {
    //            RoutedEvent = SortChangedEvent,
    //            Source = this
    //        });
    //    }
    //}

    bool dragging;
}

