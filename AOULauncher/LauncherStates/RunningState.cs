using AOULauncher.Views;

namespace AOULauncher.LauncherStates;

public class RunningState(MainWindow window) : LoadingState(window)
{
    public override void EnterState()
    {
        base.EnterState();
        Window.InstallText.Text = "Running";
        Window.ProgressBar.ProgressTextFormat = "Running...";
    }
}