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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

using Windows.Storage;

namespace FileCommander.Controls;

// TODO Favorites
// TODO Home

// TODO Connect remote drives
// TODO exif dark mode

// TODO Viewers
// TODO Drag n drop

public sealed partial class VirtualTable : UserControl
{
    public event Action<bool>? OnTab;
    internal event Func<OtherSide>? OnOtherVirtualTable;
    public event Action<string>? OnAdaptPath;

    internal Controller Controller { get; private set; } = null!;
    internal FolderContext Context { get; private set; } = null!;

    public VirtualTable() => InitializeComponent();
    public void Refresh() => SendEvent(new(Reload: new()));
    public void ToggleSelection() => SendEvent(new(ToggleSelection: new()));
    public void SelectAllAbove() => SendEvent(new(SelectAllAbove: new()));
    public void SelectAllBeneath() => SendEvent(new(SelectAllBeneath: new()));
    public void SelectAll() => SendEvent(new(SelectAll: new()));
    public void SelectNone() => SendEvent(new(SelectNone: new()));
    public async void CreateFolder() => Controller.CreateFolder(Content);
    public async void DeleteItems() => SendEvent(new(DeleteItems: new()));
    public async void Rename() => SendEvent(new(Rename: new()));
    public async void RenameAsCopy() => SendEvent(new(RenameAsCopy: new()));
    public async void Copy() => SendEvent(new(Copy: new()));
    public async void Move() => SendEvent(new(Move: new()));
    public async void AdaptPath() => SendEvent(new(AdaptPath: new()));
    public async void Execute() => SendEvent(new(Execute: new()));
    public async void ShowProperties() => SendEvent(new(ShowProperties: new()));
    public async void OpenWith() => SendEvent(new(OpenWith: new()));

    public void SetContext(FolderContext context)
    {
        this.Context = context;
        Controller = Controller.GetInitial(context);
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
    
    async void CoreWebView2_WebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
    {
        try
        {
            var path = new Uri(args.Request.Uri).AbsolutePath[1..];
            if (path.StartsWith("request"))
            await ServeRequest(path[8..], args);
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
        catch (UnauthorizedAccessException uae)
        {
            Debug.WriteLine($"Fehler aufgetreten: {uae}");
            MainWindow.ShowError(uae.Message);
            args.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(null, 500, "Handler Error", null);
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Fehler aufgetreten: {e}");
            args.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(null, 500, "Handler Error", null);
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

    async Task ServeRequest(string path, CoreWebView2WebResourceRequestedEventArgs args)
    {
        switch (path)
        {
            case "init":
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                var key = $"{Context.Id}-latestPath";
                var initpath = settings.TryGetValue(key, out var value) ? (string)value : "";
                var columns = Controller.GetColumns();
                (var items, _, var dirs, var files) = Controller.GetItems(initpath, true);
                Context.CurrentFileCount = files;
                Context.CurrentDirectoryCount = dirs;
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
                (var items, var newPos) = Controller.Sort(index, desc, subcol, pos);
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
                Controller = Controller.GetFromPath(newpath, Controller, Context);
                var (items, newPos, dirs, files) = Controller.GetItems(newpath, false);
                Context.CurrentFileCount = files;
                Context.CurrentDirectoryCount = dirs;
                var itemsResult = items != null ? new ItemsResult(null, items, newPos) : null;
                SendResult(args, new ProcessResult(ItemsResult: itemsResult));
                break;
            }
            case "getFileChanges":
            {
                var deferral = args.GetDeferral();
                try
                {
                    var items = await Controller.GetItemChangesAsync();
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
                var newPath = Context.GetHistory(query.TryGetValue("forward", out var val) && val == "true");
                ItemsResult? itemsResult = null;
                if (newPath != null)
                {
                    Controller = Controller.GetFromPath(newPath, Controller, Context);
                    var (items, newPos, dirs, files) = Controller.GetItems(newPath, false, true);
                    Context.CurrentFileCount = files;
                    Context.CurrentDirectoryCount = dirs;
                    itemsResult = items != null ? new ItemsResult(null, items, newPos) : null;
                }
                SendResult(args, itemsResult);
                break;
            }
            case "deleteItems":
            {
                var deferral = args.GetDeferral();
                try
                {
                    var stream = args.Request.Content.AsStreamForRead();
                    var items = JsonSerializer.Deserialize<int[]>(stream);
                    if (items != null)
                    {
                        var res = await Controller.DeleteItems(Content, items);
                        SendResult(args, new RequestResult(res));
                    }
                    else
                        SendResult(args, new RequestResult(false));
                    break;
                }
                finally
                {
                    deferral.Complete();
                }
            }
            case "copy":
            {
                var deferral = args.GetDeferral();
                try
                {
                    var stream = args.Request.Content.AsStreamForRead();
                    var items = JsonSerializer.Deserialize<CopyItems>(stream, Json.Defaults);
                    var otherSide = OnOtherVirtualTable?.Invoke();
                    if (items != null && otherSide != null)
                    {
                        var res = await Controller.Copy(Content, items, otherSide.Other, otherSide.IsRight);
                        SendResult(args, new RequestResult(res));
                        otherSide.Other.Refresh();
                    }
                    else
                        SendResult(args, new RequestResult(false));
                    break;
                }
                finally
                {
                    deferral.Complete();
                }
            }
            case "rename":
            {
                var deferral = args.GetDeferral();
                try
                {
                    var stream = args.Request.Content.AsStreamForRead();
                    var item = JsonSerializer.Deserialize<RenameItem>(stream, Json.Defaults);
                    var res = await Controller.Rename(Content, item?.Item ?? -1, item?.AsCopy == true);
                    SendResult(args, new RequestResult(res));
                }
                finally
                {
                    deferral.Complete();
                }
                break;
            }
            case "adaptpath":
            {
                OnAdaptPath?.Invoke(Context.CurrentPath);
                SendResult(args, new ProcessResult());
                break;    
            }
            default:
            {
                if (path.StartsWith("process"))
                {
                    var pos = int.Parse(path[8..]);
                    if (Controller.Process(pos))
                        SendResult(args, new ProcessResult());
                    else
                    {
                        var (controller, cols, newPath, _) = this.Controller.CheckPath(pos);
                        var (items, oldPos, dirCount, fileCount) = controller.GetItems(newPath, controller != this.Controller);
                        if (this.Controller != controller)
                        {
                            this.Controller.Dispose();
                            this.Controller = controller;
                        }
                        Context.CurrentFileCount = fileCount;
                        Context.CurrentDirectoryCount = dirCount;
                        var itemsResult = new ItemsResult(cols, items, oldPos);
                        SendResult(args, new ProcessResult(ItemsResult: itemsResult));
                    }
                }
                if (path.StartsWith("onposition"))
                {
                    var pos = int.Parse(path[11..]);
                    Context.SelectedPath = Controller.OnPosition(pos);
                    SendResult(args, new ProcessResult());
                }
                else if (path.StartsWith("refresh"))
                {
                    var pos = int.Parse(path[8..]);
                    var (items, newPos, dirs, files) = Controller.Refresh(pos);
                    if (items != null)
                    {
                        Context.CurrentFileCount = files;
                        Context.CurrentDirectoryCount = dirs;
                    }
                    var itemsResult = items != null ? new ItemsResult(null, items, newPos) : null;
                    SendResult(args, new ProcessResult(ItemsResult: itemsResult));
                }
                else if (path.StartsWith("reload"))
                {
                    var pos = int.Parse(path[7..]);
                    var (items, newPos, dirs, files) = Controller.Reload(pos);
                    Context.CurrentFileCount = files;
                    Context.CurrentDirectoryCount = dirs;
                    var itemsResult = items != null ? new ItemsResult(null, items, newPos) : null;
                    SendResult(args, new ProcessResult(ItemsResult: itemsResult));
                }
                else if (path.StartsWith("execute"))
                {
                    var pos = int.Parse(path[8..]);
                    Controller.Execute(pos);
                    SendResult(args, new ProcessResult());
                }
                else if (path.StartsWith("showProperties"))
                {
                    var pos = int.Parse(path[15..]);
                    Controller.OnEnter(pos, false);
                    SendResult(args, new ProcessResult());
                }
                else if (path.StartsWith("openWith"))
                {
                    var pos = int.Parse(path[9..]);
                    Controller.OnEnter(pos, true);
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
                        case "renameAsCopy":
                            MainContext.Instance.RenameAsCopyCommand.Execute(null);
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
}

record OtherSide(VirtualTable Other, bool IsRight);