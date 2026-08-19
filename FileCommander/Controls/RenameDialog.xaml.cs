using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FileCommander.Controls;

public sealed partial class RenameDialog : UserControl
{
    public string FileName
    {
        get => (string)GetValue(FileNameProperty);
        set => SetValue(FileNameProperty, value);
    }

    public static readonly DependencyProperty FileNameProperty =
        DependencyProperty.Register(nameof(FileName), typeof(string), typeof(CreateFolderDialog), new PropertyMetadata(default));

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(CreateFolderDialog), new PropertyMetadata(default));

    public RenameDialog() => InitializeComponent();

    void root_Loaded(object sender, RoutedEventArgs e) => Textbox.SelectAll();
}
