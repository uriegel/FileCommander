using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FileCommander.Controls;

public sealed partial class CreateFolderDialog : UserControl
{
    public string FolderName
    {
        get => (string)GetValue(FolderNameProperty);
        set => SetValue(FolderNameProperty, value);
    }

    public static readonly DependencyProperty FolderNameProperty =
        DependencyProperty.Register(nameof(FolderName), typeof(string), typeof(CreateFolderDialog), new PropertyMetadata(default));

    public CreateFolderDialog() => InitializeComponent();

    void root_Loaded(object sender, RoutedEventArgs e) => Textbox.SelectAll();
}
