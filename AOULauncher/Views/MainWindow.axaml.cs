using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using AOULauncher.Enum;
using AOULauncher.Tools;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Ionic.Zip;

namespace AOULauncher.Views;

public partial class MainWindow : Window
{
    public LauncherConfig Config;
    private ButtonState _buttonState;
    private List<FileHash>? _hashes;
    
    public ButtonState ButtonState
    {
        get => _buttonState;
        private set
        {
            _buttonState = value;
            UpdateButtonByState();
        }
    }
 
    private HttpClient HttpClient { get; }

    public MainWindow()
    {
        InitializeComponent();
        // Load config
        Config = File.Exists(Constants.ConfigPath)
            ? JsonSerializer.Deserialize(File.ReadAllText(Constants.ConfigPath), LauncherConfigContext.Default.LauncherConfig)
            : new LauncherConfig();
        
        var progressHandler = new ProgressMessageHandler(new HttpClientHandler {AllowAutoRedirect = true});
        progressHandler.HttpReceiveProgress += (_, args) => {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateProgress(args.ProgressPercentage);
            });
        };

        HttpClient = new HttpClient(progressHandler, true);        
        RemoveOutdatedLauncher();
        
        // start downloading launcher data and load among us path to check for mod installation
        Task.Run(DownloadData);
    }

    private void UpdateProgress(int value)
    {
        ProgressBar.Value = value;
    }
    
    private async Task DownloadData()
    {
        try
        {
            Config.ModPackData = await HttpClient.DownloadJson(Constants.ApiLocation, LauncherConfigContext.Default.ModPackData);
            _hashes = await HttpClient.DownloadJson(Constants.HashLocation, FileHashListContext.Default.ListFileHash);
            await CheckLauncherUpdate();
            await Dispatcher.UIThread.InvokeAsync(LoadAmongUsPath);
        }
        catch (Exception e)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Console.Out.WriteLine(e.ToString());
                Config.ModPackData = JsonSerializer.Deserialize(File.ReadAllText(Constants.ConfigPath), LauncherConfigContext.Default.LauncherConfig).ModPackData;
                LoadAmongUsPath();
            });
        }
    }

    private static void RemoveOutdatedLauncher()
    {
        var outdated = new FileInfo(Path.Combine(AppContext.BaseDirectory,"AOULauncher.exe.old"));

        if (outdated.Exists)
        {
            outdated.Delete();
        }
    }
    
    
    private async Task CheckLauncherUpdate()
    {
        if (!Version.TryParse(Config.ModPackData.LatestLauncherVersion, out var version))
        {
            return;
        }

        var assembly = Assembly.GetExecutingAssembly();
        await Console.Out.WriteLineAsync(assembly.GetName().Version +", "+version); 
        if (version <= assembly.GetName().Version)
        {
            return;
        }

        var file = new FileInfo(Path.Combine(AppContext.BaseDirectory,"AOULauncher.exe"));

        if (!file.Exists)
        {
            return;
        }

        file.MoveTo(file.FullName+".old", true);

        await HttpClient.DownloadFile("AOULauncher.exe", AppContext.BaseDirectory, Config.ModPackData.LauncherUpdateLink);

        Process.Start(Path.Combine(AppContext.BaseDirectory, "AOULauncher.exe"));
        Process.GetCurrentProcess().Kill();
    }
    
    private void LoadAmongUsPath()
    {
        ProgressBar.Value = 0;
        ProgressBar.ProgressTextFormat = "Loading...";
        
        if (!AmongUsLocator.VerifyAmongUsDirectory(Config.AmongUsPath))
        {
            if (AmongUsLocator.FindAmongUs() is { } path)
            {
                Config.AmongUsPath = path;
            }
            else
            {
                Console.Out.WriteLine("no among us detected");
                ButtonState = ButtonState.Refresh;
                return;
            }
        }
       
        ProgressBar.ProgressTextFormat = "";

        var bepInExPlugins = new DirectoryInfo(Path.Combine(Constants.ModFolder, "BepInEx", "plugins"));
        
        if (!bepInExPlugins.Exists)
        {
            ButtonState = ButtonState.Install;
            return;
        }
        
        var filesPresent = true;
        var updateRequired = false;
            
        foreach (var info in Config.ModPackData.ModList)
        {
            var pluginPath = Path.Combine(bepInExPlugins.FullName, info.Name);

            var updated = Utilities.IsPluginUpdated(pluginPath, info.Hash, out var exists);

            if (!exists)
            {
                Console.Out.WriteLine($"Missing {info.Name}");
                filesPresent = false;
            }

            if (!updated)
            {
                Console.Out.WriteLine($"Out of date: {info.Name}");
                updateRequired = true;
            }
        }

        if (filesPresent)
        {
            ButtonState = updateRequired ? ButtonState.Update : ButtonState.Launch;
        }
        else
        {
            ButtonState = ButtonState.Install;
        }
    }
    
    public async void InstallClickHandler(object sender, RoutedEventArgs args)
    {
        ProgressBar.Value = 0;
        switch (ButtonState)
        {
            case ButtonState.Install:
            case ButtonState.Update:
                ButtonState = ButtonState.Loading;
                await InstallMod();
                LoadAmongUsPath();
                break;
            
            case ButtonState.Launch:
                ButtonState = ButtonState.Loading;
                await Launch();
                break;
            
            case ButtonState.Refresh:
                ButtonState = ButtonState.Loading;
                LoadAmongUsPath();
                break;

            case ButtonState.Loading:
            case ButtonState.Running:
                break;

            default:
                throw new Exception("Invalid button state");
        }
    }

    // installs the mod to the ModFolder directory. see the launching logic further down for how doorstop is used
    private async Task InstallMod()
    {
        Utilities.KillAmongUs();
        
        await InstallZip("BepInEx.zip", Constants.ModFolder, Config.ModPackData.BepInEx);
        await InstallPlugins(Constants.ModFolder);
        await InstallZip("ExtraData.zip", Constants.ModFolder, Config.ModPackData.ExtraData);
    }
    
    private async Task InstallZip(string name, string directory, ModPackData.ZipData zipData)
    {
        var zipFile = await HttpClient.DownloadZip(name, Constants.DataLocation, zipData);
        
        ProgressBar.ProgressTextFormat = $"Installing {name}";
 
        zipFile.ExtractProgress += (_, args) =>
        {
            if (args.EntriesTotal != 0)
            {
                ProgressBar.Value = 100 * ((float)args.EntriesExtracted / args.EntriesTotal);
            }
        };
        zipFile.ExtractAll(directory, ExtractExistingFileAction.OverwriteSilently);
        
        ProgressBar.ProgressTextFormat = $"Installed {name}";
    }
    
    private async Task InstallPlugins(string directory)
    {
        var pluginPath = Path.Combine(directory, "BepInEx", "plugins");
        
        ProgressBar.ProgressTextFormat = "Installing mod...";
        ProgressBar.Value = 0;
        
        foreach (var plugin in Config.ModPackData.ModList)
        {
            var path = Path.Combine(pluginPath, plugin.Name);
            if (File.Exists(path) && Utilities.IsPluginUpdated(path, plugin))
            {
                continue;
            }
            ProgressBar.ProgressTextFormat = $"Downloading {plugin.Name}...";

            await HttpClient.DownloadFile(plugin.Name, pluginPath, plugin.Download);
        }
        ProgressBar.ProgressTextFormat = "Installed plugins";
    }

    private void UpdateButtonByState()
    {
        switch (ButtonState)
        {
            case ButtonState.Refresh:
                InfoIcon.IsVisible = true;
                InfoText.Foreground = Brush.Parse("#FFBB00");
                InfoText.Text = "";
                InfoText.Inlines?.Add("Among Us could not be found. Run the game and \npress refresh or click ");
                InfoText.Inlines?.Add(new Run("here") {FontWeight = FontWeight.SemiBold});
                InfoText.Inlines?.Add(" to choose manually");
                break;
                
            case ButtonState.Running:
            case ButtonState.Loading:
                InstallButton.IsEnabled = false;
                SetInfoToPath();
                break;
            
            case ButtonState.Install:
                InstallButton.IsEnabled = true;
                SetInfoToPath();
                break;
            
            case ButtonState.Update:
            case ButtonState.Launch:
            default:
                InstallButton.IsEnabled = true;
                SetInfoToPath();
                break;
        }
            
        InstallButton.IsEnabled = ButtonState != ButtonState.Running;
        InstallText.Text = _buttonState.ToString();
    }

    private void SetInfoToPath()
    {
        InfoIcon.IsVisible = false;
        InfoText.Foreground = Brush.Parse("#555");
        InfoText.Text = Config.AmongUsPath;
    }
    

    // create our own doorstop config
    private async Task SetDoorstopConfig()
    {
        var targetAssembly = Path.GetFullPath("BepInEx/core/BepInEx.Unity.IL2CPP.dll", Constants.ModFolder);
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
        if (existingCfg.Exists)
        {
            if (!existingBak.Exists)
            {
                Path.ChangeExtension(existingCfg.FullName, ".ini.bak");
            }
        }
        
        await File.WriteAllTextAsync(Path.Combine(Config.AmongUsPath, "doorstop_config.ini"), rawCfg);
    }

    private void CopyFromModToGame(string path)
    {
        File.Copy(Path.Combine(Constants.ModFolder, path), Path.Combine(Config.AmongUsPath, path), true);
    }
    
    private async Task Launch()
    {
        Utilities.KillAmongUs();
        
        // copy doorstop and set config
        CopyFromModToGame("winhttp.dll");
        await SetDoorstopConfig();
        
        ButtonState = ButtonState.Running;
        ProgressBar.ProgressTextFormat = "Running...";

        var cheater = new FileInfo(Path.GetFullPath("version.dll", Config.AmongUsPath));
        if (cheater.Exists)
        {
            cheater.MoveTo(Path.ChangeExtension(cheater.FullName, ".dll.nuhuh"));
        }

        var platform = AmongUsLocator.GetPlatform(Config.AmongUsPath, Config.ModPackData.SteamHash);

        if (platform is null)
        {
            LoadAmongUsPath();
            return;
        }

        var launcher = new AmongUsLauncher(Config.AmongUsPath, platform.Value, AmongUsOnExit);
        launcher.Launch();
    }
    
    private void AmongUsOnExit()
    {
        WindowState = WindowState.Normal;
        Topmost = true;
        Topmost = false;
        Activate();
        Show();
        
        ButtonState = ButtonState.Loading;
        Uninstall();
    }
    
    private void Uninstall()
    {
        var doorstopBackup = new FileInfo(Path.Combine(Config.AmongUsPath, "doorstop_config.ini.bak"));
        if (doorstopBackup.Exists)
        {
            doorstopBackup.MoveTo(Path.Combine(Config.AmongUsPath, "doorstop_config.ini"));
        }
        else
        {
            foreach (var file in Constants.UninstallPaths)
            {
                var info = new FileInfo(Path.Combine(Config.AmongUsPath, file));
                if (info.Exists)
                {
                    info.Delete();
                }
            }
        }

        LoadAmongUsPath();
    }
    
    private async void OpenDirectoryPicker(object? _, RoutedEventArgs e)
    {
        if (ButtonState is ButtonState.Loading or ButtonState.Running)
        {
            return;
        }
        
        var picked = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Among Us.exe",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Among Us"){Patterns = ["Among Us.exe"]}]
        });
        
        if (picked.Count <= 0)
        {
            return;
        }

        var file = new FileInfo(picked[0].Path.LocalPath);
        
        if (file.Directory is not null && AmongUsLocator.VerifyAmongUsDirectory(file.Directory.FullName))
        {
            Config.AmongUsPath = file.Directory.FullName;
            LoadAmongUsPath();
        }

    }

    private async void DiscordLinkOnClick(object? _, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://dsc.gg/allofus") {UseShellExecute = true});
        var clipboard = GetTopLevel(this)?.Clipboard;
        var dataObject = new DataObject();
        dataObject.Set(DataFormats.Text, "https://dsc.gg/allofus");
        if (clipboard is not null)
        {
            await clipboard.SetDataObjectAsync(dataObject);
        }
    }

    private void PointerDown(object? _, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }
}