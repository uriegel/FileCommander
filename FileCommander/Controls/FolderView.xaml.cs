using System;

using FileCommander.Contexts;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Windows.System;

namespace FileCommander.Controls;

public sealed partial class FolderView : UserControl
{
    public event Action? OnTab;

    public FolderContext Context { get; } = new FolderContext();

    public FolderView()
    {
        InitializeComponent();
        DataContext = Context;
        VirtualTable.SetContext(Context);
        VirtualTable.OnTab += ctrl =>
        {
            if (ctrl)
                PathTextBox.Focus(FocusState.Keyboard);
            else
                OnTab?.Invoke();
        };
    }

    public void Refresh()
        => VirtualTable.Refresh();

    void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        e.Handled = true;
        if (e.Key == VirtualKey.Tab)
            VirtualTable.Focus(FocusState.Keyboard);
    }
}
