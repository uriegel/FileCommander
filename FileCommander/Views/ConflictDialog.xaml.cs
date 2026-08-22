using FileCommander.Controllers;

using Microsoft.UI.Xaml;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using Windows.Graphics;
using Windows.System;

namespace FileCommander.Views;

// TODO Fill list
// TODO control AccentButton from conflicts and default action

public sealed partial class ConflictDialog : Window
{
    internal static Task<ConflictDialogResult> ShowAsync(IEnumerable<ConflictItem> conflicts, string action, bool fromRight) 
    {
        var window = new ConflictDialog(conflicts, $"Überschreiben beim {action}", fromRight);
        window.Activate();
        return window.completion.Task;
    }

    public bool FromRight { get; }

    ObservableCollection<ConflictItem> Items { get; } = [];

    public ConflictDialog()
    {
        InitializeComponent();
        AppWindow.Resize(new SizeInt32(1000, 800));
        ListView.ItemsSource = Items;
        No.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
        No.
    }

    internal ConflictDialog(IEnumerable<ConflictItem> conflicts, string description, bool fromRight) 
        : this()
    {
        FromRight = fromRight;
        Description.Text = description;

        foreach (var item in conflicts)
        {
            Items.Add(item);
            Items.Add(item);
            Items.Add(item);
            Items.Add(item);
            Items.Add(item);
            Items.Add(item);
        }
    }

    void Grid_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
            Close();
    }

    void root_Closed(object sender, WindowEventArgs args) => completion.TrySetResult(result);

    ConflictDialogResult result = ConflictDialogResult.Canceled;
    readonly TaskCompletionSource<ConflictDialogResult> completion = new();
}

enum ConflictDialogResult
{
    Overwrite,
    DoNotOverwrite,
    Canceled,
}
