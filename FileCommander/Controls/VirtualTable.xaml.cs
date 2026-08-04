using CsTools;
using CsTools.Extensions;

using FileCommander.Contexts;
using FileCommander.Controllers;
using FileCommander.Data;
using FileCommander.Icon;
using FileCommander.Obsoletes;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text.Json;

using static System.Net.WebRequestMethods;

namespace FileCommander.Controls;

// TODO Status bar: files, directories
// TODO restriction in index.js
// TODO Save history

// TODO Save/Reload Options https://learn.microsoft.com/en-us/windows/apps/develop/data/store-and-retrieve-app-data

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

    public void Refresh() => SendEvent(new(Reload: new()));

    public void SetContext(FolderContext context) => this.context = context;
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
                (var items, _, _, var dirs, var files) = controller.GetItems("");
                context.CurrentFileCount = files;
                context.CurrentDirectoryCount = dirs;
                var itemsResult = new ItemsResult(columns, items, 0);
                SendResult(args, itemsResult);
                break;
            }
            case "sort":
            {
                var query = MakeQuery(args.Request.Uri);
                var index = query.TryGetValue("column", out var res) ? res.ParseInt() ?? 0 : 0;
                var desc = query.TryGetValue("descending", out var descVal) ? descVal == "true" : false;
                var subcol = query.TryGetValue("subcolumn", out var colVal) ? colVal == "true" : false;
                var pos = query.TryGetValue("pos", out var resPos) ? resPos.ParseInt() ?? 0 : 0;
                (var items, var newPos) = controller.Sort(index, desc, subcol, pos);
                var itemsResult = items != null ? new ItemsResult(null, items, newPos) : null;
                SendResult(args, new ProcessResult(ItemsResult: itemsResult));
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
                        context.CurrentFileCount = res.fileCount;
                        context.CurrentDirectoryCount = res.dirCount;
                        var itemsResult = new ItemsResult(cols, res.Items, res.oldPos);
                        SendResult(args, new ProcessResult(ItemsResult: itemsResult));
                    }
                }
                if (path.StartsWith("onposition"))
                {
                    var pos = int.Parse(path[11..]);
                    context.SelectedPath = controller.OnPosition(pos);
                }
                else if (path.StartsWith("refresh"))
                {
                    var pos = int.Parse(path[8..]);
                    var (items, newPos, dirs, files) = controller.Refresh(pos);
                    if (items != null)
                    {
                        context.CurrentFileCount = files;
                        context.CurrentDirectoryCount = dirs;
                    }
                    var itemsResult = items != null ? new ItemsResult(null, items, newPos) : null;
                    SendResult(args, new ProcessResult(ItemsResult: itemsResult));
                }
                else if (path.StartsWith("reload"))
                {
                    var pos = int.Parse(path[7..]);
                    var (items, newPos, dirs, files) = controller.Reload(pos);
                    context.CurrentFileCount = files;
                    context.CurrentDirectoryCount = dirs;
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
                        case "refresh":
                            MainContext.Instance.RefreshCommand.Execute(null);
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

    // TODO to CsTools
    static ImmutableDictionary<string, string> MakeQuery(string url)
        => url.SubstringAfter('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(MakeQueryParam)
            .ToImmutableDictionary(StringComparer.OrdinalIgnoreCase);

    static KeyValuePair<string, string> MakeQueryParam(string line)
        => new(
            line.SubstringUntil('='),
            Uri.UnescapeDataString(line.SubstringAfter('=').Trim())
        );

    // TODO retrieve last path from storage
    Controller controller = Controller.GetFromPath(null, null);
    FolderContext context = null!;
}