using ClrWinApi;

using FileCommander.Contexts;
using FileCommander.Controls;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

using System;
using System.Threading.Tasks;

using Windows.ApplicationModel;
using Windows.Graphics;
using Windows.Storage;

namespace FileCommander;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        mainWindow = this;

        var settings = ApplicationData.Current.LocalSettings.Values;
        if (settings?.ContainsKey("WindowX") == true)
        {
            if (IsWindowVisible())
                AppWindow.MoveAndResize(new RectInt32(
                    (int)settings["WindowX"],
                    (int)settings["WindowY"],
                    (int)settings["WindowWidth"],
                    (int)settings["WindowHeight"]));

            if ((bool)settings["WindowMaximized"])
                ((OverlappedPresenter)AppWindow.Presenter).Maximize();
        }
        
        MainGrid.DataContext = MainContext.Instance;
        MainContext.Instance.ShowHiddenCommand = ShowHiddenCommand;
        MainContext.Instance.RefreshCommand = RefreshCommand;

        // Assumes "this" is a XAML Window. In projects that don't use 
        // WinUI 1.3 or later, use interop APIs to get the AppWindow.
        AppWindow.Changed += AppWindow_Changed;
        Activated += MainWindow_Activated;
        AppTitleBar.SizeChanged += AppTitleBar_SizeChanged;
        AppTitleBar.Loaded += AppTitleBar_Loaded;

        ExtendsContentIntoTitleBar = true;
        if (ExtendsContentIntoTitleBar == true)
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;

        // Laufzeit-Guard für API, die ab Windows 10.0.19041 verfügbar ist
        string appDisplayName = "File Commander";
        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
            appDisplayName = AppInfo.Current.DisplayInfo.DisplayName;
        TitleBarTextBlock.Text = appDisplayName;

        LeftView.OnTab += () => RightView.Focus(FocusState.Keyboard);
        RightView.OnTab += () => LeftView.Focus(FocusState.Keyboard);

        activeView = LeftView;
        MainContext.Instance.ChangeFolderContext(LeftView.Context);
        Focus();
        async void Focus()
        {
            activeView?.Focus(FocusState.Keyboard);
            await Task.Delay(100);
            activeView?.Focus(FocusState.Keyboard);
        }
    }

    public static void RunOnUI(Action action)
        => mainWindow.DispatcherQueue.TryEnqueue(() => action());

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

    void LeftView_GotFocus(object sender, RoutedEventArgs e)
    {
        activeView = sender as FolderView;
        MainContext.Instance.ChangeFolderContext(LeftView.Context);
    }

    void RightView_GotFocus(object sender, RoutedEventArgs e)
    {
        activeView = sender as FolderView;
        MainContext.Instance.ChangeFolderContext(RightView.Context);
    }

    void Grid_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Tab)
        {
            GetOtherView().Focus(FocusState.Keyboard);
            e.Handled = true;
        }
    }

    void RefreshCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => activeView?.Refresh();

    FolderView GetOtherView() => activeView == LeftView ? RightView : LeftView;

    void Window_Closed(object sender, WindowEventArgs args)
    {
        var settings = ApplicationData.Current.LocalSettings.Values;

        var presenter = (OverlappedPresenter)AppWindow.Presenter;

        if (presenter.State != OverlappedPresenterState.Maximized)
        {
            // Save these values in your settings
            settings["WindowX"] = AppWindow.Position.X;
            settings["WindowY"] = AppWindow.Position.Y;
            settings["WindowWidth"] = AppWindow.Size.Width;
            settings["WindowHeight"] = AppWindow.Size.Height;
        }

        settings["WindowMaximized"] =
            presenter.State == OverlappedPresenterState.Maximized;
    } 

    bool IsWindowVisible()
    {
        var rect = new Rect()
        { 
            Left = AppWindow.Position.X, 
            Top = AppWindow.Position.Y, 
            Right= AppWindow.Position.X + AppWindow.Size.Width, 
            Bottom = AppWindow.Position.Y + AppWindow.Size.Height 
        };
        return Api.MonitorFromRect(ref rect, MonitorDefaultTo.Null) != 0;
    }

    async void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.CodeActivated || args.WindowActivationState == WindowActivationState.PointerActivated)
        {
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => activeView?.Focus(FocusState.Programmatic));
        }
    }

    static MainWindow mainWindow = null!;
    FolderView? activeView;
}
