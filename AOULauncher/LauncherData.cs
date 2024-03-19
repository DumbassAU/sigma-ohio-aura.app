namespace AOULauncher;

public class LauncherData
{
    public string BepInEx { get; set; }
    public string LPDLLHash { get; set; }
    public string ReactorHash { get; set; }

    public override string ToString()
    {
        return $"BepInEx Version: {BepInEx} | Launchpad Hash: {LPDLLHash} | Reactor Hash: {ReactorHash}";
    }
}