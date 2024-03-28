using System;
using System.IO;

namespace AOULauncher;

public static class Constants
{
    public const string ApiLocation = "https://www.xtracube.dev/assets/js/launcherData.json";
    public const string HashLocation = "https://www.xtracube.dev/assets/js/hashes.json";
    
    public static readonly string DataLocation = Path.Combine(Environment.CurrentDirectory, "LauncherData");
    public static readonly string CachedModDirectory = Path.Combine(DataLocation, "CachedMod");
    public static readonly string ConfigPath = Path.Combine(DataLocation, "launcherConfig.json");
}