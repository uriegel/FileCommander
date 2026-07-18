using FileCommander.DataStore;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

// TODO ColumnViewHeaders: 3 fixed
// TODO ColumnViewHeaders: column width adaption
// TODO ColumnViewHeaders: column width adaption to column view
// TODO ColumnViewHeaders: Binding to date and size
// TODO ColumnViewHeaders: FileSystemWatcher for date and size
// TODO ColumnViewHeaders: FileSystemWatcher binding to list content
// TODO Key control: tab left right
// TODO Key control: up and down home/end selected item

// TODO TemplateSelector: move it to folderview, so that ColumnView is more generic 
// TODO ListItemTemplate (hidden)

namespace FileCommander.Controls;

public sealed partial class ColumnView : UserControl
{
    public ColumnView()
    {
        InitializeComponent();
    }

    internal void SetStore(Store store) => ListView.ItemsSource = store.Items;

    void ListView_KeyDown(object sender, KeyRoutedEventArgs e)
    {

    }

    void ListView_GotFocus(object sender, RoutedEventArgs e)
    {

    }

    void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

    }
}
