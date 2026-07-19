using FileCommander.DataStore;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

// TODO Key control: tab left right, Control Tab select edit field
// TODO Key control: focused side selected item red, other side gray
// TODO ColumnViewHeaders: Binding to date and size
// TODO ColumnViewHeaders: FileSystemWatcher for date and size
// TODO ColumnViewHeaders: FileSystemWatcher binding to list content
// TODO GridSplitter

// TODO TemplateSelector: move it to folderview, so that ColumnView is more generic 
// TODO ListItemTemplate (hidden)

namespace FileCommander.Controls;

public sealed partial class ColumnView : UserControl
{
    public ColumnView()
    {
        InitializeComponent();
        context = new Context();
        DataContext = context;
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

    public void ScrollCurrentIntoView()
    {
        if (ListView.TryGetElement(store?.GetIndex(context.SelectedItem)?? -1) is FrameworkElement element)
        {
            element.StartBringIntoView(bringIntoViewOptions);
        }
        else
        {
            // jump close enough to make it realized
            var index = store?.GetIndex(context.SelectedItem) ?? 0;
            Scroller.ChangeView(
                null,
                index * context.ItemsHeight,
                null,
                true); // diable animation

            //// after layout pass, bring it into view precisely
            //DispatcherQueue.TryEnqueue(() =>
            //{
            //    if (ListView.TryGetElement(index) is FrameworkElement e)
            //        e.StartBringIntoView();
            //});
        }
    }

    void ListView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Down)
        {
            var next = Math.Min(store?.GetIndex(context.SelectedItem) + 1 ?? 0, store?.GetCount() -1 ?? 0);
            context.SelectedItem = store?.Items[next];
            ScrollCurrentIntoView();
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Up)
        {
            var next = Math.Max(store?.GetIndex(context.SelectedItem) - 1 ?? 0, 0);
            context.SelectedItem = store?.Items[next];
            ScrollCurrentIntoView();
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.PageDown)
        {
            int visibleRows = (int)(Scroller.ViewportHeight / context.ItemsHeight);
            var next = Math.Min(store?.GetIndex(context.SelectedItem) + visibleRows - 1 ?? 0, store?.GetCount() - 1 ?? 0);
            context.SelectedItem = store?.Items[next];
            ScrollCurrentIntoView();
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.PageUp)
        {
            int visibleRows = (int)(Scroller.ViewportHeight / context.ItemsHeight);
            var next = Math.Max(store?.GetIndex(context.SelectedItem) - visibleRows + 1 ?? 0, 0);
            context.SelectedItem = store?.Items[next];
            ScrollCurrentIntoView();
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Home)
        {
            context.SelectedItem = store?.Items[0];
            ScrollCurrentIntoView();
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.End)
        {
            context.SelectedItem = store?.Items[store?.GetCount() - 1 ?? 0];
            ScrollCurrentIntoView();
            e.Handled = true;
        }
    }

    void ListView_GotFocus(object sender, RoutedEventArgs e)
    {

    }

    void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }

    Context context;
    Store? store;
}
