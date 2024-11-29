using Avalonia;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AOULauncher;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        TaskScheduler.UnobservedTaskException += ExceptionHandler;

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            File.AppendAllText("ErrorLog.txt", $"[{DateTime.Now}] {e}");
        }
    }

    private static void ExceptionHandler(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        File.AppendAllText("ErrorLog.txt", $"[{DateTime.Now}] {e.Exception.Message}");
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}