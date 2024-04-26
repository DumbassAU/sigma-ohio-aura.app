using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace AOULauncher;

public static class AmongUsLocator
{
    public const string EosDllRelativePath = "Among Us_Data/Plugins/x86/GfxPluginEGS.dll";
    
    public static AmongUsPlatform? GetPlatform(string path, string steamHash)
    {
        if (!VerifyAmongUsDirectory(path))
        {
            return null;
        }

        if (File.Exists(Path.GetFullPath(EosDllRelativePath, path)))
        {
            return AmongUsPlatform.Epic;
        }

        var globalGameFile = new FileInfo(Path.Combine(path, "Among Us_Data", "globalgamemanagers"));

        if (!globalGameFile.Exists)
        {
            return null;
        }
        
        var hash = Utilities.FileToHash(globalGameFile.FullName);

        return hash == steamHash ? AmongUsPlatform.Steam : AmongUsPlatform.Itch;
    }
    
    
    // return among us path by checking processes first, then registry
    public static string? FindAmongUs()
    {
        var processes = Process.GetProcessesByName("Among Us");
        if (processes.Length <= 0)
        {
            if (UpdatePathFromRegistry() is { } pathFromRegistry)
            {
                return VerifyAmongUsDirectory(pathFromRegistry) ? pathFromRegistry : null;
            }
            return null;
        }
        
        var path = Path.GetDirectoryName(processes.First().GetMainModuleFileName());
        
        return VerifyAmongUsDirectory(path) ? path : null;
    }
    
    
    // Finds among us from registry location
    private static string? UpdatePathFromRegistry()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }
        
        var registryEntry = Registry.GetValue(@"HKEY_CLASSES_ROOT\amongus\DefaultIcon", "", null);

        if (registryEntry is not string path)
        {
            return null;
        }
        
        var indexOfExe = path.LastIndexOf("Among Us.exe", StringComparison.OrdinalIgnoreCase);
        return path.Substring(1,Math.Max(indexOfExe - 1,0));

    }
    
    
    public static bool VerifyAmongUsDirectory(string? path)
    {
        return path is not null && Directory.Exists(path) && File.Exists(Path.Combine(path, "Among Us.exe"));
    }
}