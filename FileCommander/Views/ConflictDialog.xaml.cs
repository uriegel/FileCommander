using CsTools.Extensions;

using FileCommander.Controllers;
using FileCommander.ValueConverters;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Windows.Graphics;
using Windows.System;

using WinUITools.ItemsRepeaterExtensions;

namespace FileCommander.Views;


public sealed partial class ConflictDialog : Window
{
    internal static async Task<ConflictDialogResult> ShowAsync(ConflictItem[] conflicts, string action, bool fromRight) 
    {
        await ResolveIcons(conflicts);


        bool no = conflicts.Any(n => n.SourceDate < n.TargetDate || ItemsComparer.CompareVersion(n.SourceVersion, n.TargetVersion) < 0);
        var window = new ConflictDialog(conflicts, $"Überschreiben beim {action}", fromRight, no);
        window.Activate();
        return await window.completion.Task;
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
            new TextColumnViewHeader("Größe"),
            new TextColumnViewHeader("Version")
        ]);

        bool no = Items.Any(n => n.SourceDate > n.TargetDate);
        ListView.ItemsSource = Items;
    }

    internal ConflictDialog(ConflictItem[] conflicts, string description, bool fromRight, bool no) 
        : this()
    {
        this.no = no;
        FromRight = fromRight;
        Description.Text = description;
        var btn = no ? No : Yes;
        btn.Style = (Style)Application.Current.Resources["AccentButtonStyle"];

        foreach (var item in conflicts)
            Items.Add(item);

        Focus();

        async void Focus()
        {
            await Task.Delay(100);
            ListView.Focus(FocusState.Programmatic);
        }
    }

    async static Task ResolveIcons(ConflictItem[] conflicts)
    {
        foreach (var item in conflicts)
            await ShellIconCache.GetAsync(item.IconIndex);
    }

    void Grid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Escape)
            Close();
        else if (e.Key == VirtualKey.Enter)
        {
            result = no ? ConflictDialogResult.DoNotOverwrite : ConflictDialogResult.Overwrite;
            Close();
        }
    }

    void Yes_Click(object sender, RoutedEventArgs e)
    {
        result = ConflictDialogResult.Overwrite;
        Close();
    }

    void No_Click(object sender, RoutedEventArgs e)
    {
        result = ConflictDialogResult.DoNotOverwrite;
        Close();
    }

    void Dialog_Closed(object sender, WindowEventArgs args) => completion.TrySetResult(result);

    ConflictDialogResult result = ConflictDialogResult.Canceled;
    readonly bool no;
    readonly TaskCompletionSource<ConflictDialogResult> completion = new();
    readonly Navigation navigation;
}

enum ConflictDialogResult
{
    Overwrite,
    DoNotOverwrite,
    Canceled,
}
