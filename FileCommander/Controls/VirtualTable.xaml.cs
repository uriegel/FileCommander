using CsTools;
using CsTools.Extensions;

using FileCommander.Contexts;
using FileCommander.Controllers;
using FileCommander.Data;
using FileCommander.Exceptions;
using FileCommander.Icon;

using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.UI.WebUI;

namespace FileCommander.Controls;

// TODO Drag: start Drag on items in VirtualTable 
// TODO Drag: drag to external apps (Explorer, Desktop, etc.) -> use WebView2DragDrop API 
// TODO Drag: drag to other side of FileCommander -> use WebView2DragDrop API
// TODO Drag: Favorites: drag to Favorites -> use WebView2DragDrop API
// TODO Viewers



// TODO Drag:
/*
    WebWindowNetCore MASTER 18.8.2024
    Commander CSHARP 

    const onDragStart = async (evt: React.DragEvent) => {
        const items = getSelectedItems().map(n => n.name)
        if (items.length > 0) {
            setDragStarted(true)
            internalDrag = true
            evt.preventDefault()
            await webViewDragStart(path, items)
            setDragStarted(false)
            internalDrag = false
        } 
    }


 member this.StartDragFiles (fileList: string) = 
            this.DoDragDrop(DataObject(DataFormats.FileDrop, (TextJson.deserialize<DragFiles> fileList).Files), DragDropEffects.All)

 member this.onDrop (e: QueryContinueDragEventArgs) = 
        if e.Action = DragAction.Drop then
            webView.ExecuteScriptAsync "WebView.setDroppedEvent(true)"
            |> Async.AwaitTask
            |> ignore
        else if e.Action = DragAction.Cancel then
            webView.ExecuteScriptAsync "WebView.setDroppedEvent(false)"
            |> Async.AwaitTask
            |> ignore
*/

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
    public async void CreateFolder() => Controller.CreateFolder();
    public async void DeleteItems() => SendEvent(new(DeleteItems: new()));
    public async void Rename() => SendEvent(new(Rename: new()));
    public async void RenameAsCopy() => SendEvent(new(RenameAsCopy: new()));
    public async void Copy() => SendEvent(new(Copy: new()));
    public async void Move() => SendEvent(new(Move: new()));
    public async void AdaptPath() => SendEvent(new(AdaptPath: new()));
    public async void Execute() => SendEvent(new(Execute: new()));
    public async void ShowProperties() => SendEvent(new(ShowProperties: new()));
    public async void OpenWith() => SendEvent(new(OpenWith: new()));
    public async void ShowFavorites() => SendEvent(new(ShowFavorites: new()));

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

        WebView.AllowDrop = true;
        WebView.DragStarting += WebView_DragStarting;
        WebView.CoreWebView2.AddWebResourceRequestedFilter("https:*", CoreWebView2WebResourceContext.All);
        WebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
        WebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
        WebView.Source = new Uri("https://localhost/index.html");
        MainContext.Instance.PropertyChanged += MainContext_PropertyChanged;
    }

    private async void WebView_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            string[] filePaths = [@"C:\Users\urieg\OneDrive\Fitness\Fitness.txt", @"C:\Users\urieg\OneDrive\Fitness\Fitness-ghostface.txt"];
            var files = await filePaths
                .ToAsyncEnumerable()
                .Select(async (string n, CancellationToken c) => await StorageFile.GetFileFromPathAsync(n))
                .ToArrayAsync();


            args.Data.SetStorageItems(files);
            args.DragUI.SetContentFromSoftwareBitmap(CreateDragIcon(234));
            args.Data.RequestedOperation = DataPackageOperation.Copy | DataPackageOperation.Move;

            static SoftwareBitmap CreateDragIcon(int count)
            {
                const int size = 64;
                var pixels = new byte[size * size * 4];

                void Pixel(int x, int y, byte r, byte g, byte b)
                {
                    if (x < 0 || x >= size || y < 0 || y >= size)
                        return;

                    int i = (y * size + x) * 4;
                    pixels[i] = b;
                    pixels[i + 1] = g;
                    pixels[i + 2] = r;
                    pixels[i + 3] = 255;
                }

                void Rect(int x, int y, int w, int h,
                    byte r, byte g, byte b)
                {
                    for (int py = y; py < y + h; py++)
                        for (int px = x; px < x + w; px++)
                            Pixel(px, py, r, g, b);
                }

                void Circle(int cx, int cy, int radius,
                    byte r, byte g, byte b)
                {
                    for (int y = -radius; y <= radius; y++)
                        for (int x = -radius; x <= radius; x++)
                            if (x * x + y * y <= radius * radius)
                                Pixel(cx + x, cy + y, r, g, b);
                }

                // Document
                Rect(13, 6, 31, 43, 245, 245, 245);
                Rect(34, 6, 10, 10, 190, 190, 190);
                Rect(19, 22, 19, 2, 100, 100, 100);
                Rect(19, 29, 19, 2, 100, 100, 100);
                Rect(19, 36, 13, 2, 100, 100, 100);

                // Blue count badge
                Circle(44, 44, 16, 35, 115, 220);

                // Simple 3x5 digit font
                string[] digits =
                [
                    "111101101101111", // 0
            "010110010010111", // 1
            "111001111100111", // 2
            "111001111001111", // 3
            "101101111001001", // 4
            "111100111001111", // 5
            "111100111101111", // 6
            "111001001001001", // 7
            "111101111101111", // 8
            "111101111001111"  // 9
                ];

                string number = Math.Clamp(count, 0, 99).ToString();
                int scale = 2;
                int width = number.Length * 3 * scale
                          + (number.Length - 1) * scale;
                int startX = 44 - width / 2;
                int startY = 39;

                for (int n = 0; n < number.Length; n++)
                {
                    string digit = digits[number[n] - '0'];

                    for (int i = 0; i < 15; i++)
                    {
                        if (digit[i] != '1')
                            continue;

                        Rect(
                            startX + n * 8 + (i % 3) * scale,
                            startY + (i / 3) * scale,
                            scale, scale,
                            255, 255, 255);
                    }
                }

                var buffer = CryptographicBuffer.CreateFromByteArray(pixels);

                return SoftwareBitmap.CreateCopyFromBuffer(
                    buffer,
                    BitmapPixelFormat.Bgra8,
                    size,
                    size,
                    BitmapAlphaMode.Premultiplied);
            }
        }
        finally
        {
            deferral.Complete();
        }
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
        catch (Win32Exception w32e)
        {
            MainWindow.ShowError(w32e.Message);
            args.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(null, 500, "Handler Error", null);
        }
        catch (CredentialException ce)
        {
            MainWindow.ShowError(ce.Message);
            args.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(null, 500, "Handler Error", null);
        }
        catch (IOException ioe)
        {
            MainWindow.ShowError(ioe.Message);
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
                    var deferral = args.GetDeferral();
                    try
                    {
                        var settings = ApplicationData.Current.LocalSettings.Values;
                        var key = $"{Context.Id}-latestPath";
                        var initpath = settings.TryGetValue(key, out var value) ? (string)value : "";
                        var columns = Controller.GetColumns();
                        (var items, _, var dirs, var files) = await Controller.GetItemsAsync(initpath, true);
                        Context.CurrentFileCount = files;
                        Context.CurrentDirectoryCount = dirs;
                        var itemsResult = new ItemsResult(columns, items, 0);
                        SendResult(args, itemsResult);
                    }
                    finally
                    {
                        deferral.Complete();
                    }
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
                    var deferral = args.GetDeferral();
                    try
                    {
                        var query = MakeQuery(args.Request.Uri);
                        var newpath = query.TryGetValue("path", out var res) ? res ?? "" : "";
                        var oldController = Controller;
                        Controller = Controller.GetFromPath(newpath, oldController, Context);
                        var cols = oldController != Controller ? Controller.GetColumns() : null;
                        var (items, newPos, dirs, files) = await Controller.GetItemsAsync(newpath, false);
                        Context.CurrentFileCount = files;
                        Context.CurrentDirectoryCount = dirs;
                        var itemsResult = items != null ? new ItemsResult(cols, items, newPos) : null;
                        SendResult(args, new ProcessResult(ItemsResult: itemsResult));
                    }
                    finally
                    {
                        deferral.Complete();
                    }
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
                    var deferral = args.GetDeferral();
                    try
                    {

                        var query = MakeQuery(args.Request.Uri);
                        var newPath = Context.GetHistory(query.TryGetValue("forward", out var val) && val == "true");
                        ItemsResult? itemsResult = null;
                        if (newPath != null)
                        {
                            var oldController = Controller;
                            Controller = Controller.GetFromPath(newPath, oldController, Context);
                            var cols = Controller != oldController ? Controller.GetColumns() : null;
                            var (items, newPos, dirs, files) = await Controller.GetItemsAsync(newPath, false, true);
                            Context.CurrentFileCount = files;
                            Context.CurrentDirectoryCount = dirs;
                            itemsResult = items != null ? new ItemsResult(cols, items, newPos) : null;
                        }
                        SendResult(args, itemsResult);
                    }
                    finally
                    {
                        deferral.Complete();
                    }
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
                            var res = await Controller.DeleteItems(items);
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
                            var res = await Controller.Copy(items, otherSide.Other, otherSide.IsRight);
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
                        var res = await Controller.Rename(item?.Item ?? -1, item?.AsCopy == true);
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
                        var deferral = args.GetDeferral();
                        try
                        {
                            var pos = int.Parse(path[8..]);
                            if (Controller.Process(pos))
                                SendResult(args, new ProcessResult());
                            else
                            {
                                var (controller, cols, newPath, _) = this.Controller.CheckPath(pos);
                                var (items, oldPos, dirCount, fileCount) = await controller.GetItemsAsync(newPath, controller != this.Controller);
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
                        finally
                        {
                            deferral.Complete();
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
                        var deferral = args.GetDeferral();
                        try
                        {
                            var pos = int.Parse(path[7..]);
                            var (items, newPos, dirs, files) = await Controller.ReloadAsync(pos);
                            Context.CurrentFileCount = files;
                            Context.CurrentDirectoryCount = dirs;
                            var itemsResult = items != null ? new ItemsResult(null, items, newPos) : null;
                            SendResult(args, new ProcessResult(ItemsResult: itemsResult));
                        }
                        finally
                        {
                            deferral.Complete();
                        }
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

    async void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var msg = JsonSerializer.Deserialize<MsgEvent>(e.WebMessageAsJson, Json.Defaults);
        if (msg?.Msg == 1)
        {
            var files = e.AdditionalObjects
                            .OfType<CoreWebView2File>()
                            .Select(n => n.Path)
                            .ToArray();
            await Controller.CopyFrom(files, msg.Move == true, OnOtherVirtualTable?.Invoke().IsRight == false);
        }
        else if (msg?.Msg == 2)
        {
            var point = PointerPoint.GetCurrentPoint(1);
            if (point.Properties.IsLeftButtonPressed)
                await WebView.StartDragAsync(point);
        }
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
record MsgEvent(int Msg, bool? Move, string[]? Items);

