using ClrWinApi;

using CsTools;
using CsTools.Extensions;

using FileCommander.Controller;
using FileCommander.Data;
using FileCommander.DataStore;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileCommander.Controls;

public sealed partial class VirtualTable : UserControl
{
    public VirtualTable()
    {
        InitializeComponent();
    }

    async void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        await WebView.EnsureCoreWebView2Async();

        WebView.CoreWebView2.AddWebResourceRequestedFilter("https:*", CoreWebView2WebResourceContext.All);
        WebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested; ;
        WebView.Source = new Uri("https://localhost/index.html");
    }

    void CoreWebView2_WebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
    {
        try
        {
            var path = new Uri(args.Request.Uri).AbsolutePath[1..];
            if (path.StartsWith("request"))
                ServeRequest(path[8..], args);
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
        catch (Exception ex)
        {
            args.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(null, 500, "Handler Error", null);
            return;
        }
    }

    async void ServeRequest(string path, CoreWebView2WebResourceRequestedEventArgs args)
    {
        if (path == "getItems")
        {
            var deferral = args.GetDeferral();
            try
            {
                var items = Get(@"c:\windows\system32");
                var ms = new MemoryStream();
                JsonSerializer.Serialize(ms, items, Json.Defaults);
                args.Response =
                    WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                        ms.AsRandomAccessStream(),
                        200,
                        "OK",
                        "Content-Type: application/json");
            }
            finally
            {
                deferral.Complete();
            }
        }
    }

    async void ServeIcon(string path, CoreWebView2WebResourceRequestedEventArgs args)
    {
        var deferral = args.GetDeferral();
        try
        {
            var hIcon = GetIconHandle(path, 16);
            using var icon = Icon.FromHandle(hIcon);
            using var bitmap = icon.ToBitmap();
            var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            args.Response =
                WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                    stream.AsRandomAccessStream(),
                    200,
                    "OK",
                    "Content-Type: image/png");
        }
        finally
        {
            deferral.Complete();
        }
    }

    static nint GetIconHandle(string pathOrExtension, int size)
    {
        var info = new ShFileInfo();
        var result = Api.SHGetFileInfo(pathOrExtension, ClrWinApi.FileAttributes.Normal, ref info, Marshal.SizeOf<ShFileInfo>(),
            SHGetFileInfoConstants.SYSICONINDEX | SHGetFileInfoConstants.USEFILEATTRIBUTES | SHGetFileInfoConstants.TYPENAME);
        if (result == 0)
            return 0;

        var imageListSize = size switch
        {
            <= 16 => ShilImageListSize.Small,
            <= 32 => ShilImageListSize.Large,
            <= 48 => ShilImageListSize.ExtraLarge,
            _ => ShilImageListSize.Jumbo
        };

        var guid = Guids.IID_IImageList;
        Api.SHGetImageList(imageListSize, ref guid, out IImageList? imageList);
        if (imageList == null)
            return 0;

        imageList.GetIcon(info.Icon, ImageListDrawFlags.Transparent, out IntPtr hIcon);
        return hIcon;
    }


    VTItem[] Get(string path)
    {
        var dirInfo = new DirectoryInfo(path);
        var dirs = dirInfo
                        .GetDirectories()
                        .Select(DirectoryItem.Create)
                        .OrderBy(n => n.Name)
                        .Select(n => new VTItem(null, n.Name, 0, n.DateTime))
                        .ToArray();
        var files = dirInfo
                        .GetFiles()
                        .Select(FileItem.Create)
                        .Select(n => new VTItem($"icon/{n.Name.GetFileExtension()}", n.Name, n.Size, n.DateTime))
                        .ToArray();
        return [
            new VTItem(null, "..", 0, null),
            .. dirs,
            .. files
        ];
    }
}

record VTItem(string? icon, string Name, long Size, DateTime? Date);