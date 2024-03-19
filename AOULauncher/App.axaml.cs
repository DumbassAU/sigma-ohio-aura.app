using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AOULauncher.ViewModels;
using AOULauncher.Views;
using Avalonia.Controls;

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
            var mainWindowViewModel = new MainWindowViewModel();
            var window = new MainWindow
            {
                DataContext = mainWindowViewModel,
            };
            desktop.MainWindow = window;

        }

        base.OnFrameworkInitializationCompleted();
    }
}