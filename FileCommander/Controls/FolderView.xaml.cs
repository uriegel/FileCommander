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
    }

    public void Refresh()
        => VirtualTable.Refresh();

    void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {

    }

    void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {

    }
}
