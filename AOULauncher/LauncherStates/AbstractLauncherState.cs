using System.Threading.Tasks;
using AOULauncher.Views;

namespace AOULauncher.LauncherStates;

public abstract class AbstractLauncherState(MainWindow window)
{
    protected MainWindow Window { get; } = window;
    
    protected LauncherConfig Config { get; } = window.Config;

    public virtual Task ButtonClick()
    {
        return Task.CompletedTask;
    }

    public abstract void EnterState();
}