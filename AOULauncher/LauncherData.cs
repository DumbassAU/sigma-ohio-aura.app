namespace AOULauncher;

public class LauncherData
{
    public string BepInEx { get; set; }
    public ModInfo[] ModList { get; set; }

    public override string ToString()
    {
        return $"BepInEx Version: {BepInEx}";
    }

    public class ModInfo
    {
        public string Name { get; set; }
        public string Hash { get; set; }
        public string Download { get; set; }
    }
}