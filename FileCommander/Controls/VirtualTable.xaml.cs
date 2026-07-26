using CsTools;
using CsTools.Extensions;

using FileCommander.Controller;
using FileCommander.Data;
using FileCommander.DataStore;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
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

    VTItem[] Get(string path)
    {
        var dirInfo = new DirectoryInfo(path);
        var dirs = dirInfo
                        .GetDirectories()
                        .Select(DirectoryItem.Create)
                        .OrderBy(n => n.Name)
                        .Select(n => new VTItem(n.Name, 0, n.DateTime))
                        .ToArray();
        var files = dirInfo
                        .GetFiles()
                        .Select(FileItem.Create)
                        .Select(n => new VTItem(n.Name, n.Size, n.DateTime))
                        .ToArray();
        return [
            new VTItem("..", 0, null),
            .. dirs,
            .. files
        ];
    }
}

record VTItem(string Name, long Size, DateTime? Date);