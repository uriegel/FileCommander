using System;
using System.Threading.Tasks;

using FileCommander.Controls;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Windows.ApplicationModel;

namespace FileCommander;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        // Assumes "this" is a XAML Window. In projects that don't use 
        // WinUI 1.3 or later, use interop APIs to get the AppWindow.
        AppWindow.Changed += AppWindow_Changed;
        Activated += MainWindow_Activated;
        AppTitleBar.SizeChanged += AppTitleBar_SizeChanged;
        AppTitleBar.Loaded += AppTitleBar_Loaded;
        
        ExtendsContentIntoTitleBar = true;
        if (ExtendsContentIntoTitleBar == true)
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        
        // Laufzeit-Guard für API, die ab Windows 10.0.19041 verfügbar ist
        string appDisplayName = "File Commander";
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
            appDisplayName = AppInfo.Current.DisplayInfo.DisplayName;
        TitleBarTextBlock.Text = appDisplayName;

        LeftView.OnTab += () => RightView.Focus(FocusState.Keyboard);
        RightView.OnTab += () => LeftView.Focus(FocusState.Keyboard);

        activeView = LeftView;
        Focus();

        async void Focus()
        {
            await Task.Delay(100);
            activeView.Focus(FocusState.Keyboard);
        }
        
    }

    void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
    {
        if (ExtendsContentIntoTitleBar == true)
        {
            // Set the initial interactive regions.
            SetRegionsForCustomTitleBar();
        }
    }

    void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (ExtendsContentIntoTitleBar == true)
        {
            // Update interactive regions if the size of the window changes.
            SetRegionsForCustomTitleBar();
        }
    }

    void SetRegionsForCustomTitleBar()
    {
        // Specify the interactive regions of the title bar.

        double scaleAdjustment = AppTitleBar.XamlRoot.RasterizationScale;

        RightPaddingColumn.Width = new GridLength(AppWindow.TitleBar.RightInset / scaleAdjustment);
        LeftPaddingColumn.Width = new GridLength(AppWindow.TitleBar.LeftInset / scaleAdjustment);
    }


    void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            TitleBarTextBlock.Foreground =
                (SolidColorBrush)App.Current.Resources["WindowCaptionForegroundDisabled"];
        }
        else
        {
            TitleBarTextBlock.Foreground =
                (SolidColorBrush)App.Current.Resources["WindowCaptionForeground"];
        }
    }

    void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidPresenterChange)
        {
            switch (sender.Presenter.Kind)
            {
                case AppWindowPresenterKind.CompactOverlay:
                    // Compact overlay - hide custom title bar
                    // and use the default system title bar instead.
                    AppTitleBar.Visibility = Visibility.Collapsed;
                    sender.TitleBar.ResetToDefault();
                    break;

                case AppWindowPresenterKind.FullScreen:
                    // Full screen - hide the custom title bar
                    // and the default system title bar.
                    AppTitleBar.Visibility = Visibility.Collapsed;
                    sender.TitleBar.ExtendsContentIntoTitleBar = true;
                    break;

                case AppWindowPresenterKind.Overlapped:
                    // Normal - hide the system title bar
                    // and use the custom title bar instead.
                    AppTitleBar.Visibility = Visibility.Visible;
                    sender.TitleBar.ExtendsContentIntoTitleBar = true;
                    break;

                default:
                    // Use the default system title bar.
                    sender.TitleBar.ResetToDefault();
                    break;
            }
        }
    }

    void HamburgerButton_Click(object sender, RoutedEventArgs e)
    {
        
    }

    void LeftView_GotFocus(object sender, RoutedEventArgs e)
    {
        activeView = sender as FolderView;
    }

    void RightView_GotFocus(object sender, RoutedEventArgs e)
    {
        activeView = sender as FolderView;
    }

    void Grid_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Tab)
        {
            GetOtherView().Focus(FocusState.Keyboard);
            e.Handled = true;
        }
    }

    private void SwitchPresenter(object sender, RoutedEventArgs e)
    {
        if (AppWindow != null)
        {
            AppWindowPresenterKind newPresenterKind;
            switch ((sender as Button).Name)
            {
                case "CompactoverlaytBtn":
                    newPresenterKind = AppWindowPresenterKind.CompactOverlay;
                    break;

                case "FullscreenBtn":
                    newPresenterKind = AppWindowPresenterKind.FullScreen;
                    break;

                case "OverlappedBtn":
                    newPresenterKind = AppWindowPresenterKind.Overlapped;
                    break;

                default:
                    newPresenterKind = AppWindowPresenterKind.Default;
                    break;
            }

            // If the same presenter button was pressed as the
            // mode we're in, toggle the window back to Default.
            if (newPresenterKind == AppWindow.Presenter.Kind)
            {
                AppWindow.SetPresenter(AppWindowPresenterKind.Default);
            }
            else
            {
                // Else request a presenter of the selected kind
                // to be created and applied to the window.
                AppWindow.SetPresenter(newPresenterKind);
            }
        }
    }
    FolderView GetOtherView()
        => activeView == LeftView ? RightView : LeftView;

    FolderView? activeView;
}
