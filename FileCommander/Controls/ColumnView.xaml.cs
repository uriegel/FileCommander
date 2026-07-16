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
using System.Text.Json.Serialization;
using FileCommander.DataStore;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

// TODO TemplateSelector in Resource: https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/data-template-selector
// TODO TemplateSelector: move it to folderview, so that ColumnView is more generic 
// TODO ListItemTempate (hidden)

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
