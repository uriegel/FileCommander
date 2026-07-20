using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

using FileCommander.Controller;
using System.Threading.Tasks;

using FileCommander.DataStore;

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
            //await (new DirectoryController(store)).ChangePathAsync(@"C:\users\Urieg");
            await (new DirectoryController(store, (ColumnView.DataContext as Context)!)).ChangePathAsync(@"C:\windows\system32");
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
