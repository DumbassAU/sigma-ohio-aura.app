using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Ionic.Zip;
using Newtonsoft.Json;

namespace AOULauncher.Views;

public partial class MainWindow : Window
{
    private readonly HttpClient _httpClient;
    public LauncherConfig Config;
    private ButtonState _buttonState;

    public ButtonState ButtonState
    {
        get => _buttonState;
        private set
        {
            _buttonState = value;
            UpdateButtonByState();
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        
        // Load config
        Config = File.Exists(Constants.ConfigPath)
            ? JsonConvert.DeserializeObject<LauncherConfig>(File.ReadAllText(Constants.ConfigPath))
            : new LauncherConfig();

        // initialize http client and progress handler
        var ph = new ProgressMessageHandler(new HttpClientHandler{AllowAutoRedirect = true});
        ph.HttpReceiveProgress += (_, args) =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressBar.Value = args.ProgressPercentage;
            });
        };
        
        _httpClient = new HttpClient(ph);
        
        // start downloading launcher data and load among us path to check for mod installation
        var t = DownloadData();
        t.GetAwaiter().OnCompleted(LoadAmongUsPath);
    }

    private async Task DownloadData()
    {
        try
        {
            Config.ModPackData = await _httpClient.DownloadJson<ModPackData>(Constants.ApiLocation);
        }
        catch (Exception e)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Console.Out.WriteLine(e.ToString());
                Config.ModPackData = JsonConvert.DeserializeObject<LauncherConfig>(File.ReadAllText(Constants.ConfigPath)).ModPackData;
                LoadAmongUsPath();
            });
        }
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
       
        Console.Out.WriteLine(Path.Combine(Config.AmongUsPath, "Among Us.exe"));
        ProgressBar.ProgressTextFormat = "";

        var bepInExPlugins = new DirectoryInfo(Path.Combine(Constants.CachedModDirectory,"BepInEx","plugins"));

        
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
                Console.Out.WriteLine(info.Name);
                filesPresent = false;
            }

            if (!updated)
            {
                Console.Out.WriteLine(info.Name);
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
                await InstallMod();
                
                Launch();
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

    private async Task InstallMod()
    {
        Utilities.KillAmongUs();
        
        await InstallZip("BepInEx.zip", Constants.CachedModDirectory, Config.ModPackData.BepInEx);
        await InstallPlugins(Constants.CachedModDirectory);
        await InstallZip("ExtraData.zip", Constants.CachedModDirectory, Config.ModPackData.ExtraData);
    }   
    

    private async Task InstallZip(string name, string directory, ModPackData.ZipData zipData)
    {
        var zipFile = await _httpClient.DownloadZip(name, Constants.DataLocation, zipData);
        
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

            await _httpClient.DownloadFile(plugin.Name, pluginPath, plugin.Download);
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
            case ButtonState.Install:
            case ButtonState.Update:
            case ButtonState.Launch:
            case ButtonState.Loading:
            default:
                InfoIcon.IsVisible = false;
                InfoText.Foreground = Brush.Parse("#555");
                InfoText.Text = Config.AmongUsPath;
                break;
        }
            
        InstallButton.IsEnabled = ButtonState != ButtonState.Running;
        InstallText.Text = _buttonState.ToString();
    }
    
    private void Launch()
    {
        Utilities.KillAmongUs();

        Utilities.BackupFolder(Path.Combine(Config.AmongUsPath,"BepInEx"), "plugins");
        Utilities.BackupFolder(Path.Combine(Config.AmongUsPath,"BepInEx"), "config");
         
        Utilities.CopyDirectory(Constants.CachedModDirectory, Config.AmongUsPath);
        
        ButtonState = ButtonState.Running;
        ProgressBar.ProgressTextFormat = "Running...";
        
        var process = Process.Start(Path.Combine(Config.AmongUsPath,"Among Us.exe")); 
        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => Dispatcher.UIThread.InvokeAsync(AmongUsOnExit);
    }

    private void AmongUsOnExit()
    {
        Utilities.RestoreBackupFolder(Path.Combine(Config.AmongUsPath,"BepInEx"), "plugins");
        Utilities.RestoreBackupFolder(Path.Combine(Config.AmongUsPath,"BepInEx"), "config");       
        
        ButtonState = ButtonState.Loading;
        LoadAmongUsPath();
    }

    private async void OpenDirectoryPicker(object? _, RoutedEventArgs e)
    {
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