using ClrWinApi;

using FileCommander.Contexts;
using FileCommander.Controls;

using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

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


        var kontext = MainContext.Instance;


        var test = "Test";

        MainGrid.DataContext = kontext;
        MainContext.Instance.ShowHiddenCommand = ShowHiddenCommand;
        MainContext.Instance.RefreshCommand = RefreshCommand;
        MainContext.Instance.ToggleSelectionCommand = ToggleSelectionCommand;
        MainContext.Instance.SelectAllAboveCommand = SelectAllAboveCommand;
        MainContext.Instance.SelectAllBeneathCommand = SelectAllBeneathCommand;
        MainContext.Instance.SelectAllCommand = SelectAllCommand;
        MainContext.Instance.SelectNoneCommand = SelectNoneCommand;
        MainContext.Instance.CreateFolderCommand = CreateFolderCommand;
        MainContext.Instance.RenameCommand = RenameCommand;
        MainContext.Instance.RenameAsCopyCommand = RenameAsCopyCommand;
        MainContext.Instance.AdaptPathCommand = AdaptPathCommand;
        MainContext.Instance.FavoritesCommand = FavoritesCommand;
        
        // Assumes "this" is a XAML Window. In projects that don't use 
        // WinUI 1.3 or later, use interop APIs to get the AppWindow.
        AppWindow.Changed += AppWindow_Changed;
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

    public void RestoreWindowSettings()
    {
        var settings = ApplicationData.Current.LocalSettings.Values;
        if (settings != null
            && settings.ContainsKey("WindowX") 
            && settings.ContainsKey("WindowY") 
            && settings.ContainsKey("WindowWidth") 
            && settings.ContainsKey("WindowHeight")
            && settings.ContainsKey("WindowMaximized")
            && (int)settings["WindowWidth"] > 10)
        {
            if (IsWindowVisible())
                AppWindow.MoveAndResize(new RectInt32(
                    (int)settings["WindowX"],
                    (int)settings["WindowY"],
                    (int)settings["WindowWidth"],
                    (int)settings["WindowHeight"]));

            if ((bool)settings["WindowMaximized"]
                    && AppWindow.Presenter is OverlappedPresenter presenter
                    && !presenter.State.HasFlag(OverlappedPresenterState.Maximized))
                ((OverlappedPresenter)AppWindow.Presenter).Maximize();
        }
    }

    public static void RunOnUI(Action action)
        => mainWindow.DispatcherQueue?.TryEnqueue(() => action());

    public static async void ShowError(string message)
    {
        mainWindow.MessageBar.Title = "Fehler";
        mainWindow.MessageBar.Message = message;
        mainWindow.MessageBar.Severity = InfoBarSeverity.Error;
        mainWindow.MessageBar.IsOpen = true;
        await Task.Delay(5000);
        mainWindow.MessageBar.IsOpen = false;
    }

    public static FolderView GetOtherView(FolderView thisView)
        => mainWindow.LeftView == thisView ? mainWindow.RightView : mainWindow.LeftView;

    public static bool IsRightView(FolderView thisView) => mainWindow.RightView == thisView;

    public static new UIElement Content { get => mainWindow.MainGrid; }

    public static FolderContext GetOtherContext(FolderContext context) 
        => mainWindow.LeftView.Context == context 
            ? mainWindow.RightView.Context 
            : mainWindow.LeftView.Context;

    public static void Refresh() => mainWindow.activeView?.Refresh();

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
    void ToggleSelectionCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => activeView?.ToggleSelection();
    void SelectAllAboveCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => activeView?.SelectAllAbove();
    void SelectAllBeneathCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => activeView?.SelectAllBeneath();
    void SelectAllCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => activeView?.SelectAll();
    void SelectNoneCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => activeView?.SelectNone();
    void CreateFolderCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => activeView?.CreateFolder();
    void DeleteCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => activeView?.DeleteItems();
    void RenameCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => activeView?.Rename();
    void RenameAsCopyCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => activeView?.RenameAsCopy();
    void CopyCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => activeView?.Copy();
    void MoveCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => activeView?.Move();
    void AdaptPathCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => activeView?.AdaptPath();
    void ExecuteCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => activeView?.Execute();
    void ShowPropertiesCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => activeView?.ShowProperties();
    void OpenWithCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => activeView?.OpenWith();
    void FavoritesCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => activeView?.ShowFavorites();

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
            Right = AppWindow.Position.X + AppWindow.Size.Width,
            Bottom = AppWindow.Position.Y + AppWindow.Size.Height
        };
        return Api.MonitorFromRect(ref rect, MonitorDefaultTo.Null) != 0;
    }

    void Splitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        => SetHighlight(true);

    void Splitter_PointerExited(object sender, PointerRoutedEventArgs e)
        => SetHighlight(false);

    void Splitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(MainGrid);

        if (point.Properties.IsLeftButtonPressed)
        {
            dragStartX = point.Position.X;

            leftStartWidth = LeftColumn.ActualWidth;
            rightStartWidth = RightColumn.ActualWidth;

            Splitter.CapturePointer(e.Pointer);
            IsPointerCaptured = true;

            e.Handled = true;
        }
    }

    void Splitter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!IsPointerCaptured)
            return;

        var point = e.GetCurrentPoint(MainGrid);

        var delta = point.Position.X - dragStartX;

        var newLeftWidth = leftStartWidth + delta;
        var newRightWidth = rightStartWidth - delta;

        const double minWidth = 30;

        if (newLeftWidth < minWidth)
        {
            newLeftWidth = minWidth;
            newRightWidth = leftStartWidth + rightStartWidth - minWidth;
        }

        if (newRightWidth < minWidth)
        {
            newRightWidth = minWidth;
            newLeftWidth = leftStartWidth + rightStartWidth - minWidth;
        }

        LeftColumn.Width =
               new GridLength(newLeftWidth, GridUnitType.Star);
        RightColumn.Width =
            new GridLength(newRightWidth, GridUnitType.Star);

        e.Handled = true;
    }

    void Splitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (IsPointerCaptured)
        {
            Splitter.ReleasePointerCapture(e.Pointer);
            IsPointerCaptured = false;
        }

        e.Handled = true;
    }

    void SetHighlight(bool visible)
    {
        var animation = new DoubleAnimation
        {
            To = visible ? 1 : 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(250))
        };

        Storyboard.SetTarget(animation, SplitterHighlight);
        Storyboard.SetTargetProperty(animation, "Opacity");

        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);

        storyboard.Begin();
    }

    async void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.CodeActivated || args.WindowActivationState == WindowActivationState.PointerActivated)
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                if (!Dialog.IsOpen)
                    activeView?.Focus(FocusState.Programmatic);
            });
        if (args.WindowActivationState == WindowActivationState.Deactivated)
            TitleBarTextBlock.Foreground = (SolidColorBrush)App.Current.Resources["WindowCaptionForegroundDisabled"];
        else
            TitleBarTextBlock.Foreground = (SolidColorBrush)App.Current.Resources["WindowCaptionForeground"];
    }

    static MainWindow mainWindow = null!;
    FolderView? activeView;
    readonly InputSystemCursor cursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    double dragStartX;
    double leftStartWidth;
    double rightStartWidth;
    bool IsPointerCaptured;
}

record Favorite(string Name, string Path);