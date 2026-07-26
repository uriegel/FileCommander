using CsTools;
using CsTools.Extensions;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

using System;
using System.IO;
using System.Reflection;

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
        catch (Exception ex)
        {
            args.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(null, 500, "Handler Error", null);
            return;
        }    
    }
}
