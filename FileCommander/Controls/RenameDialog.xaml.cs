using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

    void root_Loaded(object sender, RoutedEventArgs e)
    {
        var pos = FileName.LastIndexOf('.');
        if (pos == -1)
            Textbox.SelectAll();
        else
            Textbox.Select(0, pos);
    }
}
