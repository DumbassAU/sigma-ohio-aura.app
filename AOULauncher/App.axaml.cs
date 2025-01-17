using System;
using System.IO;
using System.Text.Json;
using AOULauncher.LauncherStates;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AOULauncher.Views;

namespace AOULauncher;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                var window = new MainWindow();
                desktop.MainWindow = window;

                Directory.CreateDirectory(Constants.DataLocation);
            
                window.Closing += (_, args) =>
                {
                    args.Cancel = window.LauncherState is RunningState;
                    if (!args.Cancel)
                    {
                        File.WriteAllText(Constants.ConfigPath, JsonSerializer.Serialize(window.Config, LauncherConfigContext.Default.LauncherConfig));
                    }
                };
            }
            catch (Exception e)
            {
                var window = new Error(e.ToString());
                desktop.MainWindow = window;
                window.Show();
                window.Activate();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}