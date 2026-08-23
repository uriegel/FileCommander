using FileCommander.Controllers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Windows.Graphics;
using Windows.System;

using WinUITools.ItemsRepeaterExtensions;

namespace FileCommander.Views;


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
        navigation = new Navigation(ListView, Scroller);
        AppWindow.Resize(new SizeInt32(1000, 800));
        
        Headers.SetColumns([
            new TextColumnViewHeader("Name"),
            new TextColumnViewHeader("Datum"),
            new TextColumnViewHeader("Größe")
        ]);

        bool no = Items.Any(n => n.SourceDate > n.TargetDate);
        ListView.ItemsSource = Items;
        var btn = no ? No : Yes;
        btn.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
    }

    internal ConflictDialog(IEnumerable<ConflictItem> conflicts, string description, bool fromRight) 
        : this()
    {
        FromRight = fromRight;
        Description.Text = description;

        foreach (var item in conflicts)
            Items.Add(item);
    }

    void Grid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
            Close();
    }

    void root_Closed(object sender, WindowEventArgs args) => completion.TrySetResult(result);

    ConflictDialogResult result = ConflictDialogResult.Canceled;
    readonly TaskCompletionSource<ConflictDialogResult> completion = new();
    readonly Navigation navigation;
}

enum ConflictDialogResult
{
    Overwrite,
    DoNotOverwrite,
    Canceled,
}
