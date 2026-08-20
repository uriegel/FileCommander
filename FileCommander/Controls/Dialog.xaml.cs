using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using System;
using System.Threading.Tasks;

namespace FileCommander.Controls;

public sealed partial class Dialog : UserControl
{
    public static bool IsOpen { get; private set; }

    public static async Task<TResult?> ShowAsync<TResult>(
        UIElement content,
        string title,
        Func<ContentDialog, TResult> getResult,
        UserControl? xamlContent = null, 
        string? textContent = null) where TResult: class
    {
        IsOpen = true;
        var dialog = new ContentDialog
        {
            Title = title,
            Content = (object?)xamlContent ?? new TextBlock() { Text = textContent },
            PrimaryButtonText = "Ok",
            CloseButtonText = "Abbrechen",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = content.XamlRoot
        };
        var res = await dialog.ShowAsync() == ContentDialogResult.Primary
               ? getResult(dialog)
               : null;
        IsOpen = false;
        return res;
    }

    public static async Task<bool> ShowAsync(
        UIElement content,
        string title,
        UserControl? xamlContent = null,
        string? textContent = null)
    {
        return ShowAsync(
        content,
        title,
        d => (object)true,
        xamlContent,
        textContent) == (object)true;
    }


    public Dialog()
    {
        InitializeComponent();
    }
}
