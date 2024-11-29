using System.Threading.Tasks;
using AOULauncher.Views;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace AOULauncher.LauncherStates;

public class RefreshState(MainWindow window) : AbstractLauncherState(window)
{
    public override Task ButtonClick()
    {
        Window.LauncherState = new LoadingState(Window);
        Window.LoadAmongUsPath();
        return Task.CompletedTask;
    }

    public override void EnterState()
    {
        Window.InstallButton.IsEnabled = true;
        Window.InstallText.Text = "Refresh";
        Window.InfoIcon.IsVisible = true;
        Window.InfoText.Foreground = Brush.Parse("#FFBB00");
        Window.InfoText.Text = "";
        Window.InfoText.Inlines?.Add("Among Us could not be found. Run the game and \npress refresh or click ");
        Window.InfoText.Inlines?.Add(new Run("here") {FontWeight = FontWeight.SemiBold});
        Window.InfoText.Inlines?.Add(" to choose manually");
    }
}