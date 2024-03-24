using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace AOULauncher.Views;

public partial class MainWindow : Window
{
    private string _amongUsPath = "";
    private ButtonState _buttonState;
    private LauncherData _launcherData;
    private readonly HttpClient _httpClient;
    private bool _showProgress = false;
    
    public ButtonState ButtonState
    {
        get => _buttonState;
        set
        {
            _buttonState = value;

            switch (value)
            {
                case ButtonState.Refresh:
                    InfoIcon.IsVisible = true;
                    InfoText.Foreground = Brush.Parse("#FFBB00");
                    InfoText.Text = "Could not find Among Us path. Run the game and press refresh";
                    break;
                case ButtonState.Running:
                case ButtonState.Install:
                case ButtonState.Update:
                case ButtonState.Launch:
                default:
                    InfoIcon.IsVisible = false;
                    InfoText.Foreground = Brush.Parse("#555");
                    InfoText.Text = _amongUsPath;
                    break;
            }
            
            InstallButton.IsEnabled = value != ButtonState.Running;
            InstallText.Text = _buttonState.ToString();
        }
    }

    
    public MainWindow()
    {
        InitializeComponent();
        var handler = new HttpClientHandler { AllowAutoRedirect = true };
        var ph = new ProgressMessageHandler(handler);

        ph.HttpReceiveProgress += (_, args) =>
        {
            ProgressBar.Value = args.ProgressPercentage;
        };
        
        _httpClient = new HttpClient(ph);
        
        DownloadData();
    }
    
    public void DownloadData()
    {
        var response = _httpClient.Send(new HttpRequestMessage(HttpMethod.Get, "https://www.xtracube.dev/assets/js/launcherData.json"));
        using var reader = new StreamReader(response.Content.ReadAsStream());
        _launcherData = JsonConvert.DeserializeObject<LauncherData>(reader.ReadToEnd())!;

        Console.Out.WriteLine(_launcherData.ToString());
        Dispatcher.UIThread.Post(LocateAmongUs);
    }

    public void LocateAmongUs()
    {
        var processes = Process.GetProcessesByName("Among Us");
        if (processes.Length < 1)
        {
            UpdatePathFromRegistry();
        }
        else
        {
            UpdateFromPath(Path.GetDirectoryName(processes.First().GetMainModuleFileName()));
        }
        
    }
    
    public void UpdatePathFromRegistry()
    {
        var registryEntry = Registry.GetValue(@"HKEY_CLASSES_ROOT\amongus\DefaultIcon", "", null);

        // Auto load Among Us path from Registry
        if (registryEntry is string path)
        {
            var indexOfExe = path.LastIndexOf("Among Us.exe", StringComparison.OrdinalIgnoreCase);
            var auFolder = path.Substring(1,Math.Max(indexOfExe - 1,0));
            UpdateFromPath(auFolder);
        }
        else
        {
            ButtonState = ButtonState.Refresh;
        }
    }

    public void UpdateFromPath(string? path)
    {
        if (path != null && Path.Exists(Path.Combine(path, "Among Us.exe")))
        {
            _amongUsPath = path;
            LoadAmongUsPath();
            return;
        }
        ButtonState = ButtonState.Refresh;
    }

    public void LoadAmongUsPath()
    {
        Console.Out.WriteLine(Path.Combine(_amongUsPath,"Among Us.exe"));

        var bepInPluginPath = Path.Combine(_amongUsPath, "BepInEx", "plugins");
        
        if (Path.Exists(bepInPluginPath))
        {
            var filesPresent = true;
            var updateRequired = false;
            
            foreach (var info in _launcherData.ModList)
            {
                var pluginPath = Path.Combine(bepInPluginPath, info.Name);

                var updated = IsPluginUpdated(pluginPath, info.Hash, out var exists);
                
                if (!exists)
                {
                    filesPresent = false;
                }
                
                if (!updated)
                {
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
        else
        {
            ButtonState = ButtonState.Install;
        }
    }
    
    public void InstallClickHandler(object sender, RoutedEventArgs args)
    {
        switch (ButtonState)
        {
            case ButtonState.Install:
                var task = Install();
                break;
            
            case ButtonState.Update:
                break;
            
            case ButtonState.Launch:
                Launch();
                break;
            
            case ButtonState.Refresh:
                LocateAmongUs();
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
        
    }

    private async Task Install()
    {
        var bepInExUrl = $"https://builds.bepinex.dev/api/projects/bepinex_be/artifacts/{_launcherData.BepInEx}";
       
        var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, bepInExUrl));
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync());
        
        var data = JsonConvert.DeserializeObject<BuildBE>(await reader.ReadToEndAsync())!;
        
        await Console.Out.WriteLineAsync(data.artifacts[0].file);
    }

    private void Launch()
    {
        ButtonState = ButtonState.Running;
        
        var process = Process.Start(Path.Combine(_amongUsPath,"Among Us.exe")); 
        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => Dispatcher.UIThread.InvokeAsync(AmongUsOnExit);
    }

    private void AmongUsOnExit()
    {
        LoadAmongUsPath();
        Show();
    }
    
    private static bool IsPluginUpdated(string path, string hash, out bool present)
    {
        present = false;
        if (!Path.Exists(path)) return false;
        
        present = true;
        var array = SHA256.HashData(File.ReadAllBytes(path));
        var arrayHash = string.Concat(array.Select(x => x.ToString("x2")));
        return arrayHash.Equals(hash, StringComparison.OrdinalIgnoreCase);
    }
}