using System.IO;
using System.Threading.Tasks;
using AOULauncher.Tools;
using AOULauncher.Views;

namespace AOULauncher.LauncherStates;

public class LaunchState(MainWindow window) : AbstractLauncherState(window)
{
    public override async Task ButtonClick()
    {
        Utilities.KillAmongUs();
        
        // copy doorstop and set config
        CopyFromModToGame("winhttp.dll");
        await SetDoorstopConfig();

        Window.LauncherState = new RunningState(Window);

        var cheater = new FileInfo(Path.GetFullPath("version.dll", Config.AmongUsPath));
        if (cheater.Exists)
        {
            cheater.MoveTo(Path.ChangeExtension(cheater.FullName, ".dll.no"));
        }

        var platform = AmongUsLocator.GetPlatform(Config.AmongUsPath, Config.ModPackData.SteamHash);

        if (platform is null)
        {
            Window.LoadAmongUsPath();
            return;
        }

        var launcher = new AmongUsLauncher(Config.AmongUsPath, platform.Value, Window.AmongUsOnExit);
        launcher.Launch();
    }

    public override void EnterState()
    {
        Window.InstallText.Text = "Launch";
        Window.InstallButton.IsEnabled = true;
        Window.SetInfoToPath();
    }

    // create our own doorstop config
    private async Task SetDoorstopConfig()
    {
        var targetAssembly = Path.Combine(Constants.ModFolder, "BepInEx", "core", "BepInEx.Unity.IL2CPP.dll");
        var coreclrDir = Path.Combine(Constants.ModFolder, "dotnet");
        var coreclrPath = Path.Combine(Constants.ModFolder, "dotnet", "coreclr.dll");

        var rawCfg = $"""
                      [General]
                      enabled = true
                      target_assembly = {targetAssembly}
                      [Il2Cpp]
                      coreclr_path = {coreclrPath}
                      corlib_dir = {coreclrDir}
                      """;
        var existingCfg = new FileInfo(Path.Combine(Config.AmongUsPath, "doorstop_config.ini"));
        var existingBak = new FileInfo(Path.Combine(Config.AmongUsPath, "doorstop_config.ini.bak"));
        if (existingCfg.Exists && !existingBak.Exists)
        {
            existingCfg.MoveTo(Path.ChangeExtension(existingCfg.FullName, ".ini.bak"));
        }
        
        await File.WriteAllTextAsync(Path.Combine(Config.AmongUsPath, "doorstop_config.ini"), rawCfg);
    }

    private void CopyFromModToGame(string path)
    {
        File.Copy(Path.Combine(Constants.ModFolder, path), Path.Combine(Config.AmongUsPath, path), true);
    }
}