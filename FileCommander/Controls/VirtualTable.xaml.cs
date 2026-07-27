using CsTools;
using CsTools.Extensions;

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

// TODO Folder icons
// TODO Questions: sorting in view or controller? View has to know isMounted, column type, rules parent directory...
// TODO I think sorting in the controller
// TODO Then this is not necessary: interpret Values and display them (isMounted??? opacity)

// TODO processItem request => perhaps getFiles ...

// TODO path control
//      * styled like javascript
//      * optional in javascript
// TODO Tab control viewLeft -> viewRight, to path edit
// TODO Grid Splitter (maybe WinUITools)
// TODO getRoot
// TODO getFiles

// TODO Responsibilities:
//  items C# with idx as handle
//  displayItems in webview    
//  displayItems with displayTypes (text, date, size, version)
//  displayItems sorting, filtering in webview
//  displayItems webview is using maps and arrays of objects
//  Sort by text or indexed sort kind (date, size, text, ext)
//  
public sealed partial class VirtualTable : UserControl
{
    public VirtualTable() => InitializeComponent();

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
        catch
        {
            args.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(null, 500, "Handler Error", null);
            return;
        }
    }

    async void ServeRequest(string path, CoreWebView2WebResourceRequestedEventArgs args)
    {
        switch (path)
        {
            case "getItems":
                var items = controller.GetItems();
                var ms = new MemoryStream();
                JsonSerializer.Serialize(ms, items, Json.Defaults);
                args.Response =
                    WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                        ms.AsRandomAccessStream(),
                        200,
                        "OK",
                        "Content-Type: application/json");
                break;
            case "init":
                // TODO retrieve last path from storage
                controller = Controller.GetFromPath(null, null);
                var columns = controller.GetColumns();
                SendEvent(new(GetItems: new GetItems(columns)));
                args.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(null, 200, "OK", null);
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
        }
    }

    void ServeIcon(string path, CoreWebView2WebResourceRequestedEventArgs args)
        => args.Response =
                WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                    Icons.Get(path).AsRandomAccessStream(),
                    200,
                    "OK",
                    "Content-Type: image/png");

    void SendEvent(Event evt)
        => WebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(evt, Json.Defaults));

    Controller controller = null!;
}