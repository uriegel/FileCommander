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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

using Windows.Storage;

namespace FileCommander.Controls;
 
// TODO Rename
// TODO DeleteItems (if not to trash show dialog)
// TODO Exception handling => banner
// TODO Exception handling CreateDirectory
// TODO Exception handling Delete
// TODO Exception handling GetItems
// TODO Copy: check conflicts
// TODO Copy: 
// TODO Copy: conflicts: show Conflict Dialog
// TODO Move
// TODO MainWindow activates: only focus to webview when there is no dialog

// TODO Viewers
// TODO Home folder (later Favorites, Remotes)

// TODO exif dark mode

// TODO File SystemWatcher start exif and version: after create check exifs from all newly create items, until creation time is older than 10s

public sealed partial class VirtualTable : UserControl
{
    public event Action<bool>? OnTab;

    public VirtualTable() => InitializeComponent();

    public void Refresh() => SendEvent(new(Reload: new()));
    public void ToggleSelection() => SendEvent(new(ToggleSelection: new()));
    public void SelectAllAbove() => SendEvent(new(SelectAllAbove: new()));
    public void SelectAllBeneath() => SendEvent(new(SelectAllBeneath: new()));
    public void SelectAll() => SendEvent(new(SelectAll: new()));
    public void SelectNone() => SendEvent(new(SelectNone: new()));
    public async void CreateFolder() => controller.CreateFolder(Content);
    public async void DeleteItems() => SendEvent(new(DeleteItems: new()));
    public async void Rename() => SendEvent(new(Rename: new()));

    public void SetContext(FolderContext context)
    {
        this.context = context;
        controller = Controller.GetInitial(context);
    }

    internal void SendEvent(Event evt)
        => WebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(evt, Json.Defaults));

    async void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        await WebView.EnsureCoreWebView2Async();

        WebView.CoreWebView2.AddWebResourceRequestedFilter("https:*", CoreWebView2WebResourceContext.All);
        WebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested; 
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
                var settings = ApplicationData.Current.LocalSettings.Values;
                var key = $"{context.Id}-latestPath";
                var initpath = settings.TryGetValue(key, out var value) ? (string)value : "";
                var columns = controller.GetColumns();
                (var items, _, var dirs, var files) = controller.GetItems(initpath, true);
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
                var desc = query.TryGetValue("descending", out var descVal) && descVal == "true";
                var subcol = query.TryGetValue("subcolumn", out var colVal) && colVal == "true";
                var pos = query.TryGetValue("pos", out var resPos) ? resPos.ParseInt() ?? 0 : 0;
                (var items, var newPos) = controller.Sort(index, desc, subcol, pos);
                var itemsResult = items != null ? new ItemsResult(null, items, newPos) : null;
                SendResult(args, new ProcessResult(ItemsResult: itemsResult));
                break;
            }
            case "tab":
            {
                var query = MakeQuery(args.Request.Uri);
                OnTab?.Invoke(query.TryGetValue("shift", out _));
                break;
            }
            case "changePath":
            {
                var query = MakeQuery(args.Request.Uri);
                var newpath = query.TryGetValue("path", out var res) ? res ?? "" : "";
                controller = Controller.GetFromPath(newpath, controller, context);
                var (items, newPos, dirs, files) = controller.GetItems(newpath, false);
                context.CurrentFileCount = files;
                context.CurrentDirectoryCount = dirs;
                var itemsResult = items != null ? new ItemsResult(null, items, newPos) : null;
                SendResult(args, new ProcessResult(ItemsResult: itemsResult));
                break;
            }
            case "getFileChanges":
            {
                var deferral = args.GetDeferral();
                try
                {
                    var items = await controller.GetItemChangesAsync();
                    SendResult(args, new ChangesResult(items));
                }
                finally
                {
                    deferral.Complete();
                }
                break;
            }
            case "history":
            {
                var query = MakeQuery(args.Request.Uri);
                var newPath = context.GetHistory(query.TryGetValue("forward", out var val) && val == "true");
                ItemsResult? itemsResult = null;
                if (newPath != null)
                {
                    controller = Controller.GetFromPath(newPath, controller, context);
                    var (items, newPos, dirs, files) = controller.GetItems(newPath, false, true);
                    context.CurrentFileCount = files;
                    context.CurrentDirectoryCount = dirs;
                    itemsResult = items != null ? new ItemsResult(null, items, newPos) : null;
                }
                SendResult(args, itemsResult);
                break;
            }
            case "deleteItems":
            {
                var stream = args.Request.Content.AsStreamForRead();
                var items = JsonSerializer.Deserialize<int[]>(stream);
                if (items != null)
                    controller.DeleteItems(Content, items);
                SendResult(args, new ProcessResult());
                break;
            }
            default:
            {
                if (path.StartsWith("process"))
                {
                    var pos = int.Parse(path[8..]);
                    if (controller.Process(pos))
                        SendResult(args, new ProcessResult());
                    else
                    {
                        var (controller, cols, newPath, _) = this.controller.CheckPath(pos);
                        var (items, oldPos, dirCount, fileCount) = controller.GetItems(newPath, controller != this.controller);
                        if (this.controller != controller)
                        {
                            this.controller.Dispose();
                            this.controller = controller;
                        }
                        context.CurrentFileCount = fileCount;
                        context.CurrentDirectoryCount = dirCount;
                        var itemsResult = new ItemsResult(cols, items, oldPos);
                        SendResult(args, new ProcessResult(ItemsResult: itemsResult));
                    }
                }
                if (path.StartsWith("onposition"))
                {
                    var pos = int.Parse(path[11..]);
                    context.SelectedPath = controller.OnPosition(pos);
                    SendResult(args, new ProcessResult());
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
                else if (path.StartsWith("rename"))
                {
                    var pos = int.Parse(path[7..]);
                    controller.Rename(Content, pos);
                    SendResult(args, new ProcessResult());
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
                        case "toggleSelection":
                            MainContext.Instance.ToggleSelectionCommand.Execute(null);
                            SendResult(args, new ProcessResult());
                            break;
                        case "selectAllAbove":
                            MainContext.Instance.SelectAllAboveCommand.Execute(null);
                            SendResult(args, new ProcessResult());
                            break;
                        case "selectAllBeneath":
                            MainContext.Instance.SelectAllBeneathCommand.Execute(null);
                            SendResult(args, new ProcessResult());
                            break;
                        case "selectAll":
                            MainContext.Instance.SelectAllCommand.Execute(null);
                            SendResult(args, new ProcessResult());
                            break;
                        case "selectNone":
                            MainContext.Instance.SelectNoneCommand.Execute(null);
                            SendResult(args, new ProcessResult());
                            break;
                        case "createFolder":
                            MainContext.Instance.CreateFolderCommand.Execute(null);
                            SendResult(args, new ProcessResult());
                            break;
                        case "rename":
                            MainContext.Instance.RenameCommand.Execute(null);
                            SendResult(args, new ProcessResult());
                            break;
                    }
                }
                break;
            }
        }
    }

    async void ServeIcon(string path, CoreWebView2WebResourceRequestedEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            var stream = await Task.Run(() => Icons.GetAsync(path));
            args.Response =
                        WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                            stream.AsRandomAccessStream(),
                            200,
                            "OK",
                            "Content-Type: image/png");
        }
        catch (Exception e)
        {
            var t = e;
        }
        finally
        {
            deferral.Complete();
        }
    }

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

    Controller controller = null!;
    FolderContext context = null!;
}