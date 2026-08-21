using FileCommander.Contexts;
using FileCommander.Data;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using System;

using Windows.System;

namespace FileCommander.Controls;

public sealed partial class FolderView : UserControl
{
    public static readonly DependencyProperty IdProperty = DependencyProperty.Register(
        nameof(Id), typeof(string), typeof(FolderView), new PropertyMetadata(""));
    public string Id
    {
        get => (string)GetValue(IdProperty);
        set => SetValue(IdProperty, value);
    }

    public event Action? OnTab;

    public FolderContext Context { get; private set; } = null!;

    public FolderView()
    {
        InitializeComponent();
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

    public void Refresh() => VirtualTable.Refresh();
    public void ToggleSelection() => VirtualTable.ToggleSelection();
    public void SelectAllAbove() => VirtualTable.SelectAllAbove();
    public void SelectAllBeneath() => VirtualTable.SelectAllBeneath();
    public void SelectAll() => VirtualTable.SelectAll();
    public void SelectNone() => VirtualTable.SelectNone();
    public void CreateFolder() => VirtualTable.CreateFolder();
    public void DeleteItems() => VirtualTable.DeleteItems();
    public void Rename() => VirtualTable.Rename();
    public void RenameAsCopy() => VirtualTable.RenameAsCopy();
    public void Copy() => VirtualTable.Copy();
    public void Move() => VirtualTable.Move();

    void UserControl_Loaded(object _, RoutedEventArgs e)
    {
        Context = new FolderContext(Id);
        VirtualTable.SetContext(Context);
        DataContext = Context;
    }

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

    void PathTextBox_GotFocus(object sender, RoutedEventArgs e)
        => PathTextBox.SelectAll();
}
