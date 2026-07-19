using FileCommander.Data;
using FileCommander.DataStore;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

// TODO Key control: tab left right, Control Tab select edit field

// TODO scrolling: Sometimes grid widths are not correct

// TODO ColumnViewHeaders: Binding to date and size
// TODO ColumnViewHeaders: FileSystemWatcher for date and size
// TODO ColumnViewHeaders: FileSystemWatcher binding to list content

// TODO ListItemTemplate binding to IsHidden

// TODO Selection binding with keyboard (Shortcuts)

// TODO GridSplitter

// TODO WinUITools as nuget package

// TODO TemplateSelector: move it to folderview, so that ColumnView is more generic 
// TODO ListItemTemplate (hidden)

namespace FileCommander.Controls;

public sealed partial class ColumnView : UserControl
{
    public ColumnView()
    {
        InitializeComponent();
        context = new Context();
        context.PropertyChanged += Context_PropertyChanged;
        DataContext = context;
    }

    void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Context.SelectedItem))
        {
            var next = store?.GetIndex(context.SelectedItem);
            if (next.HasValue)
                ScrollCurrentIntoView(next.Value);
        }
    }

    internal void SetStore(Store store)
    {
        ListView.ItemsSource = store.Items;
        this.store = store;
    }

    readonly BringIntoViewOptions bringIntoViewOptions = new()
    {
        AnimationDesired = false
    };

    public void ScrollCurrentIntoView(int pos, bool end = false)
    {
        lastSelectedItemPos = pos;
        if (ListView.TryGetElement(pos) is FrameworkElement element)
        {
            element.StartBringIntoView(bringIntoViewOptions);
            element.Focus(FocusState.Keyboard);
        }
        else
        {
            Debug.WriteLine($"Springe nach {pos}, {pos * (context.ItemsHeight)}");
            // jump close enough to make it realized
            Scroller.ChangeView(
                null,
                end == false ? pos * (context.ItemsHeight) : Scroller.ScrollableHeight,
                null,
                true); // disable animation

            Run(true);
            async void Run(bool first)
            {
                await Task.Delay(100);
                Debug.WriteLine($"Will holen");
                if (ListView.TryGetElement(pos) is FrameworkElement e)
                {
                    Debug.WriteLine($"geholt");
                    e.StartBringIntoView(bringIntoViewOptions);
                    e.Focus(FocusState.Keyboard);
                }
                else 
                {
                    await Task.Delay(100);
                    Scroller.ChangeView(
                        null,
                        end == false ? pos * (context.ItemsHeight) : Scroller.ScrollableHeight,
                        null,
                        true); // disable animation
                    if (first)
                        Run(false);
                }
            }
        }
    }

    void ListView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Down)
        {
            var focused = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
            if (focused is ItemGrid grid && grid.DataContext is Item item)
            {
                var next = Math.Min(store?.GetIndex(item) + 1 ?? 0, store?.GetCount() - 1 ?? 0);
                ScrollCurrentIntoView(next);
                e.Handled = true;
            }
        }
        else if (e.Key == Windows.System.VirtualKey.Up)
        {
            var focused = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
            if (focused is ItemGrid grid && grid.DataContext is Item item)
            {
                var next = Math.Max(store?.GetIndex(item) - 1 ?? 0, 0);
                ScrollCurrentIntoView(next);
                e.Handled = true;
            }
        }
        else if (e.Key == Windows.System.VirtualKey.PageDown)
        {
            var focused = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
            if (focused is ItemGrid grid && grid.DataContext is Item item)
            {
                int visibleRows = (int)(Scroller.ViewportHeight / context.ItemsHeight);
                var next = Math.Min(store?.GetIndex(item) + visibleRows - 1 ?? 0, store?.GetCount() - 1 ?? 0);
                ScrollCurrentIntoView(next);
                e.Handled = true;
            }
        }
        else if (e.Key == Windows.System.VirtualKey.PageUp)
        {
            var focused = FocusManager.GetFocusedElement(XamlRoot) as DependencyObject;
            if (focused is ItemGrid grid && grid.DataContext is Item item)
            {
                int visibleRows = (int)(Scroller.ViewportHeight / context.ItemsHeight);
                var next = Math.Max(store?.GetIndex(item) - visibleRows + 1 ?? 0, 0);
                ScrollCurrentIntoView(next);
            }
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Home)
        {
            ScrollCurrentIntoView(0);
            e.Handled = true;
        }

        else if (e.Key == Windows.System.VirtualKey.End)
        {
            ScrollCurrentIntoView(store?.GetCount() - 1 ?? 0, true);
            e.Handled = true;
        }
    }

    Context context;
    Store? store;
    int lastSelectedItemPos;

    void Scroller_GotFocus(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine($"Got fokus: {lastSelectedItemPos}");
        ScrollCurrentIntoView(lastSelectedItemPos);
    }
}
