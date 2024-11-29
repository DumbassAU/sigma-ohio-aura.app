using AOULauncher.Views;

namespace AOULauncher.LauncherStates;

public class UpdateState(MainWindow window) : InstallState(window)
{
    public override void EnterState()
    {
        base.EnterState();
        Window.InstallText.Text = "Update";
    }
}