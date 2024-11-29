using AOULauncher.Views;

namespace AOULauncher.LauncherStates;

public class LoadingState(MainWindow window) : AbstractLauncherState(window)
{
    public override void EnterState()
    {
        Window.InstallText.Text = "Loading...";
        Window.InstallButton.IsEnabled = false;
        Window.SetInfoToPath();
    }
}