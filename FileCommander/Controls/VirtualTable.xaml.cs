using CsTools;
using CsTools.Extensions;

using FileCommander.Contexts;
using FileCommander.Controllers;
using FileCommander.Data;
using FileCommander.Icon;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace FileCommander.Controls;

// TODO Refresh in menu -> send event -> reload

// TODO Sorting: sort changed -> (path and) items
// TODO restriction

// TODO Save/Reload Options

// TODO exif date and version
// TODO File SystemWatcher with directories
// TODO Tab control shift tab -> path edit
// TODO path control
//      * styled like javascript
//      * optional in javascript if not possible in XAML
// TODO Grid Splitter (maybe WinUITools)
// TODO SetSelections
// TODO Home folder (later Favorites, Remotes)
public sealed partial class VirtualTable : UserControl
{
    public event Action? OnTab;

    public VirtualTable() => InitializeComponent();

    async void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        await WebView.EnsureCoreWebView2Async();

        WebView.CoreWebView2.AddWebResourceRequestedFilter("https:*", CoreWebView2WebResourceContext.All);
        WebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested; ;
        WebView.Source = new Uri("https://localhost/index.html");
        MainContext.Instance.PropertyChanged += MainContext_PropertyChanged;
    }

    void CoreWebView2_WebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
    {
        try
        {
            var path = new Uri(args.Request.Uri).AbsolutePath[1..];
            if (path.StartsWith("request"))
                ServeRequest(path[8..], args);
            else if (path.StartsWith("iconFromRes"))
                ServeIconFromRes(path[12..], args);
            else if (path.StartsWith("icon"))
                ServeIcon(path[5..], args);
            else
            {
                var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
                if (stream != null)
                {
                    var contentType = MimeType.Get(path.GetFileExtension()) ?? "text/plain";

                    args.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                        stream.AsRandomAccessStream(), 200, "OK", $"Content-Type: {contentType}");
                    return;
                }
                else
                {
                    args.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(null, 404, "Not Found", null);
                    return;
                }
            }
        }
        catch
        {
            args.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(null, 500, "Handler Error", null);
            return;
        }
    }

    void MainContext_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainContext.ShowHidden):
                SendEvent(new(Refresh: new()));
                break;
        }
    }

    async void ServeRequest(string path, CoreWebView2WebResourceRequestedEventArgs args)
    {
        switch (path)
        {
            case "init":
            {
                var columns = controller.GetColumns();
                (var items, _, _) = controller.GetItems("");
                var itemsResult = new ItemsResult(columns, items, 0);
                SendResult(args, itemsResult);
                break;
            }
            case "tab":
                OnTab?.Invoke();
                break;
            case "TODO":
                var deferral = args.GetDeferral();
                try
                {
                    // args.Response = await ...()
                }
                finally
                {
                    deferral.Complete();
                }
                break;
            default:
            {
                    if (path.StartsWith("process"))
                    {
                        var pos = int.Parse(path[8..]);
                        if (controller.Process(pos))
                            SendResult(args, new ProcessResult());
                        else
                        {
                            (controller, var cols, var newPath, var oldPath) = controller.CheckPath(pos);
                            var res = controller.GetItems(newPath);
                            var itemsResult = new ItemsResult(cols, res.Items, res.oldPos);
                            SendResult(args, new ProcessResult(ItemsResult: itemsResult));
                        }
                    }
                    else if (path.StartsWith("refresh"))
                    {
                        var pos = int.Parse(path[8..]);
                        var (items, newPos) = controller.Refresh(pos);
                        var itemsResult = items != null ? new ItemsResult(null, items, newPos) : null;
                        SendResult(args, new ProcessResult(ItemsResult: itemsResult));
                    }
                    else if (path.StartsWith("command"))
                    {
                        var cmd = path[8..];
                        switch (cmd)
                        {
                            case "toggleHidden":
                                MainContext.Instance.ShowHidden = !MainContext.Instance.ShowHidden;
                                MainContext.Instance.ShowHiddenCommand.Execute(null);
                                SendResult(args, new ProcessResult());
                                break;
                        }
                    }
                break;
            }
        }
    }

    void ServeIcon(string path, CoreWebView2WebResourceRequestedEventArgs args)
        => args.Response =
                WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                    Icons.Get(path).AsRandomAccessStream(),
                    200,
                    "OK",
                    "Content-Type: image/png");

    void ServeIconFromRes(string path, CoreWebView2WebResourceRequestedEventArgs args)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        args.Response =
                WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                    stream.AsRandomAccessStream(),
                    200,
                    "OK",
                    "Content-Type: image/png");
    }

    void SendResult<T>(CoreWebView2WebResourceRequestedEventArgs args, T t)
    {
        var ms = new MemoryStream();
        JsonSerializer.Serialize(ms, t, Json.Defaults);
        args.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(ms.AsRandomAccessStream(),
            200, "OK", "Content-Type: application/json");
    }

    void SendEvent(Event evt)
        => WebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(evt, Json.Defaults));

    // TODO retrieve last path from storage
    Controller controller = Controller.GetFromPath(null, null);
}