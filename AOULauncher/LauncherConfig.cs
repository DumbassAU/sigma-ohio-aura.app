namespace AOULauncher;

public struct LauncherConfig(string amongUsPath, bool showConsole)
{
    public string AmongUsPath { get; set; } = amongUsPath;
    public bool ShowConsole { get; set; } = showConsole;
}