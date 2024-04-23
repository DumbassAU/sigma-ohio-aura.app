using System;
using System.IO;

namespace AOULauncher;

public static class Constants
{
    public const string ApiLocation = "https://www.xtracube.dev/assets/js/launcherData.json";
    public const string HashLocation = "https://www.xtracube.dev/assets/js/hashes.json";
    
    public static readonly string DataLocation = Path.Combine(Environment.CurrentDirectory, "LauncherData");
    public static readonly string CachedBepInEx = Path.Combine(DataLocation, "CachedBepInEx");
    public static readonly string ModFolder = Path.Combine(DataLocation, "Modpack");
    public static readonly string ConfigPath = Path.Combine(DataLocation, "launcherConfig.json");

    public static string[] UninstallPaths = ["BepInEx","dotnet",".doorstop_version","changelog.txt","doorstop_config.ini","winhttp.dll"];
}