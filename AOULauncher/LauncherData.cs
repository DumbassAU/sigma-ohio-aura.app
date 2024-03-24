using System.Linq;

namespace AOULauncher;

public struct LauncherData
{
    public ZipData BepInEx { get; set; }
    
    public ZipData ExtraData { get; set; }
    
    public ModInfo[] ModList { get; set; }

    public override string ToString()
    {
        return $"BepInEx Link: {BepInEx.Link} | Mod count: {ModList.Length}";
    }

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