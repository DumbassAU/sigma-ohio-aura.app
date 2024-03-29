namespace AOULauncher;

public struct LauncherConfig
{
    public string AmongUsPath { get; set; }
    public ModPackData ModPackData { get; set; }
    
}

public struct ModPackData
{
    public ZipData BepInEx { get; set; }
    
    public ZipData ExtraData { get; set; }
    
    public ModInfo[] ModList { get; set; }

    public struct ZipData
    {
        public string Link { get; set; }
        
        public string Hash { get; set; }
    }
    
    public struct ModInfo
    {
        public string Name { get; set; }
        
        public string Hash { get; set; }
        
        public string Download { get; set; }
    }
}