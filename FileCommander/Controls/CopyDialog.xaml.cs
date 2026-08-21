using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FileCommander.Controls;

public sealed partial class CopyDialog : UserControl
{
    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }
    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(CopyDialog), new PropertyMetadata(default));

    public bool FromRight
    {
        get => (bool)GetValue(FromRightProperty);
        set => SetValue(FromRightProperty, value);
    }
    public static readonly DependencyProperty FromRightProperty =
        DependencyProperty.Register(nameof(FromRight), typeof(bool), typeof(CopyDialog), new PropertyMetadata(default));
    
    public CopyDialog()
    {
        InitializeComponent();
    }
}
