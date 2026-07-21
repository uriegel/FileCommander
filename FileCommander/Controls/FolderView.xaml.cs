using FileCommander.Controller;
using FileCommander.DataStore;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FileCommander.Controls;

public sealed partial class FolderView : UserControl
{
    public FolderView()
    {
        InitializeComponent();
        ColumnView.SetStore(store);

        var _ = Test();
        
        async Task Test()
        {
            await (new DirectoryController(store, (ColumnView.DataContext as Context)!)).ChangePathAsync(@"C:\users\Urieg");
            //await (new DirectoryController(store, (ColumnView.DataContext as Context)!)).ChangePathAsync(@"C:\windows\system32");
            //await (new DirectoryController(store)).ChangePathAsync(@"C:\windows\");
        }
    }

    //public void SetItemsSource(IEnumerable<Item> items)
    //{
    //    //var oldView = CollectionViewSource.GetDefaultView(ColumnView.ListView.ItemsSource) as ListCollectionView;
    //    //var view = new ListCollectionView(items.ToList())
    //    //{
    //    //    CustomSort = oldView?.CustomSort,
    //    //    Filter = i => FilterHidden(i) && FilterRestriction(i),
    //    //};
    //    //ColumnView.ListView.ItemsSource = view;
    //}

    Store store = new();

    void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {

    }

    void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {

    }
}
