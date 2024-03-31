using System.Net.Http;
using System.Net.Http.Handlers;
using AOULauncher.Views;
using Avalonia.Threading;

namespace AOULauncher;

public static class NetworkManager
{
    public static HttpClient HttpClient { get; private set; }

    static NetworkManager()
    {
        var progressHandler = new ProgressMessageHandler(new HttpClientHandler {AllowAutoRedirect = true});
        progressHandler.HttpReceiveProgress += (_, args) => {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                MainWindow.Instance?.UpdateProgress(args.ProgressPercentage);
            });
        };

        HttpClient = new HttpClient(progressHandler, true);
    }
}