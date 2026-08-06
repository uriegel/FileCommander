using System;

using FileCommander.Contexts;
using FileCommander.Data;

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
        VirtualTable.SetContext(Context);
        DataContext = Context;
        VirtualTable.OnTab += ctrl =>
        {
            if (ctrl)
            {
                PathTextBox.SelectAll();
                PathTextBox.Focus(FocusState.Keyboard);
            }
            else
                OnTab?.Invoke();
        };
    }

    public void Refresh()
        => VirtualTable.Refresh();

    void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Tab)
        {
            VirtualTable.Focus(FocusState.Keyboard);
            e.Handled = true;
        }
        else if (e.Key == VirtualKey.Enter)
        {
            VirtualTable.SendEvent(new Data.Event(ChangePath: new ChangePath(PathTextBox.Text)));
            VirtualTable.Focus(FocusState.Keyboard);
            e.Handled = true;
        }
    }
}
