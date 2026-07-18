using CsTools.Extensions;

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using System.ComponentModel;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FileCommander.Controls;

public sealed partial class ColumnViewHeader : UserControl
{
    public ColumnViewHeader()
    {
        InitializeComponent();
    }

    void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        //var props = e.GetCurrentPoint((UIElement)sender).Properties;

        //if (props.IsLeftButtonPressed)
        //{
        //    MainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
        //    var child = new TextBlock()
        //    {
        //        Text = "Hallo Welt"
        //    };
        //    Grid.SetColumn(child, MainGrid.ColumnDefinitions.Count - 1);

        //    MainGrid.Children.Add(child);
        //}
        //else
        //{
        //    MainGrid.ColumnDefinitions.RemoveAt(MainGrid.ColumnDefinitions.Count - 1);
        //    MainGrid.Children.RemoveAt(MainGrid.Children.Count - 1);
        //}
    }

    void Border_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        //((UIElement)sender).InputCursor =
        //    InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }

    void Border_PointerExited(object sender, PointerRoutedEventArgs e)
    {
//         ((FrameworkElement)sender).Cursor = null;
    }
}

public class BorderGrid : Grid
{
    public BorderGrid()
    {
        this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        PointerPressed += (s, e) =>
        {
            var ja = CapturePointer(e.Pointer);

            var grid = (Grid)((Parent as Grid)!.Parent!);
            var startPos = e.GetCurrentPoint(grid).Position.X;

            var index = (int)Parent.GetValue(ColumnProperty);
            var elements = ((Parent as Grid)?.Parent as Grid)?.Children;
            var startWidth = elements![index].ActualSize.X;
            var startWidth2 = elements![index + 1].ActualSize.X;

            var i = 0; foreach (var element in elements)
                ((Parent as Grid)?.Parent as Grid).ColumnDefinitions[i++].Width = new GridLength(element.ActualSize.X, GridUnitType.Star);

            PointerReleased += Cancelled;
            PointerMoved += Moved;

            void Cancelled(object? o, PointerRoutedEventArgs e)
            {
                PointerReleased -= Cancelled;
                PointerMoved -= Moved;
            }

            void Moved(object? o, PointerRoutedEventArgs e)
            {
                var context = DataContext as Context;
                var grid = (Grid)((Parent as Grid)!.Parent!);
                var diff = e.GetCurrentPoint(grid).Position.X - startPos;
                var size1 = startWidth + diff;
                var size2 = startWidth2 -diff;
                Debug.WriteLine($"Moved: {size1}, {size2} | {startWidth2}");
                ((Parent as Grid)?.Parent as Grid).ColumnDefinitions[index].Width = new GridLength(size1, GridUnitType.Star);
                ((Parent as Grid)?.Parent as Grid).ColumnDefinitions[index+1].Width = new GridLength(size2, GridUnitType.Star);
                context?.ColumnWidths = [
                    ((Parent as Grid)?.Parent as Grid).ColumnDefinitions[0].Width,
                    ((Parent as Grid)?.Parent as Grid).ColumnDefinitions[1].Width,
                    ((Parent as Grid)?.Parent as Grid).ColumnDefinitions[2].Width
                ];      
            }
        };
    }
}

class Context : INotifyPropertyChanged
{
    public GridLength[] ColumnWidths
    {
        get;
        set
        {
            field = value;
            OnChanged(nameof(ColumnWidths));
        }
    } = [
            new GridLength(1, GridUnitType.Star),
            new GridLength(1, GridUnitType.Star),
            new GridLength(1, GridUnitType.Star)
        ];

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnChanged(string name) => PropertyChanged?.Invoke(this, new(name));
}