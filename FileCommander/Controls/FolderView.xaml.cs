using System;
using System.Threading.Tasks;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace FileCommander.Controls;

public sealed partial class FolderView : UserControl
{
    public event Action? OnTab;

    public FolderView()
    {
        InitializeComponent();
        VirtualTable.OnTab += () => OnTab?.Invoke();
        //ColumnView.SetStore(store);

        var _ = Test();
        
        async Task Test()
        {
            //await (new DirectoryController(store, (ColumnView.DataContext as Context)!)).ChangePathAsync(@"C:\users\Urieg");
            //await (new DirectoryController(store, (ColumnView.DataContext as Context)!)).ChangePathAsync(@"C:\windows\system32");
            //await (new DirectoryController(store, (ColumnView.DataContext as Context)!)).ChangePathAsync(@"C:\windows\");
        }
    }

    //public void SetItemsSource(IEnumerable<Item> items)
    //{
    //    //var oldView = CollectionViewSource. GetDefaultView(ColumnView.ListView.ItemsSource) as ListCollectionView;
    //    //var view = new ListCollectionView(items.ToList())
    //    //{
    //    //    CustomSort = oldView?.CustomSort,
    //    //    Filter = i => FilterHidden(i) && FilterRestriction(i),
    //    //};
    //    //ColumnView.ListView.ItemsSource = view;
    //}

    void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {

    }

    void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {

    }
}
