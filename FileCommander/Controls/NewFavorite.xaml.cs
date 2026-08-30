using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FileCommander.Controls;

public sealed partial class NewFavorite : UserControl
{
    public static readonly DependencyProperty FavoriteNameProperty = DependencyProperty.Register(
        nameof(FavoriteName), typeof(string), typeof(NewFavorite), new PropertyMetadata(""));
    public string FavoriteName
    {
        get => (string)GetValue(FavoriteNameProperty);
        set => SetValue(FavoriteNameProperty, value);
    }

    public static readonly DependencyProperty PathProperty = DependencyProperty.Register(
        nameof(Path), typeof(string), typeof(NewFavorite), new PropertyMetadata(""));
    public string Path
    {
        get => (string)GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public NewFavorite() => InitializeComponent();

    void TextBox_GotFocus(object sender, RoutedEventArgs e)
        => (sender as TextBox)?.SelectAll();
}
