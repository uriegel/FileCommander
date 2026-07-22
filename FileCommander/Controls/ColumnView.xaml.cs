using FileCommander.Data;
using FileCommander.DataStore;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using WinUITools.ItemsRepeaterExtensions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

// TODO ColumnViewHeaders: FileSystemWatcher for date and size
// TODO ColumnViewHeaders: FileSystemWatcher binding to list content

// TODO Selection binding with keyboard (Shortcuts)

// TODO GridSplitter

// TODO WinUITools as nuget package

// TODO DateTime/Exif converter

// TODO TemplateSelector: move it to folderview, so that ColumnView is more generic 
// TODO ListItemTemplate (hidden)

namespace FileCommander.Controls;

public sealed partial class ColumnView : UserControl
{
    public ColumnView()
    {
        InitializeComponent();
        navigation = new Navigation(ListView, Scroller);

        Headers.SetColumns([
            new TextColumnViewHeader("Name"),
            new TextColumnViewHeader("Datum"),
            new TextColumnViewHeader("Größe")
        ]);

        ListView.ItemsSource = Enumerable.Range(1, 100_000).Select(n => new Item { Name = $"Item # {n}"}).ToArray();


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

    Context context;
    Store? store;
    Navigation navigation;

    void Scroller_GotFocus(object sender, RoutedEventArgs e)
    {
//        Debug.WriteLine($"Got fokus: {lastSelectedItemPos}");
//        ScrollCurrentIntoView(lastSelectedItemPos);
    }

    private void Scroller_GettingFocus(UIElement sender, GettingFocusEventArgs args)
    {
        try
        {
            //if (ListView.TryGetElement(lastSelectedItemPos) is FrameworkElement element)
            //    args.NewFocusedElement = element;
        }
        catch (Exception e)
        {

        }
    }
}
