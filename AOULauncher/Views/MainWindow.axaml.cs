using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Ionic.Zip;
using Microsoft.Win32;
using Newtonsoft.Json;
using ZipFile = Ionic.Zip.ZipFile;

namespace AOULauncher.Views;

public partial class MainWindow : Window
{
    private const string BepInBase = "https://builds.bepinex.dev/projects/bepinex_be/";
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
                    InfoText.Text = "";
                    InfoText.Inlines.Add("Among Us could not be found. Run the game and \npress refresh or click ");
                    InfoText.Inlines.Add(new Run("here") {FontWeight = FontWeight.SemiBold});
                    InfoText.Inlines.Add(" to choose manually");
                    InfoButton.Background = Brush.Parse("#444");
                    break;
                
                case ButtonState.Running:
                case ButtonState.Install:
                case ButtonState.Update:
                case ButtonState.Launch:
                default:
                    InfoIcon.IsVisible = false;
                    InfoText.Foreground = Brush.Parse("#555");
                    InfoText.Text = _amongUsPath;
                    InfoButton.Background = Brushes.Transparent;
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
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressBar.Value = args.ProgressPercentage;
            });
        };
        
        _httpClient = new HttpClient(ph);
        
        Task.Run(DownloadData);
    }


    private T? DownloadJson<T>(string url)
    {
        var response = _httpClient.Send(new HttpRequestMessage(HttpMethod.Get, url));
        using var reader = new StreamReader(response.Content.ReadAsStream());
        return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
    }

    private async Task DownloadFile(string name, string directory, string url)
    {
        var s = await _httpClient.GetStreamAsync(url);
        await using var fs = new FileStream(Path.GetFullPath(name,directory), FileMode.OpenOrCreate);
        await s.CopyToAsync(fs);
    }
    
    
    public async Task DownloadData()
    {
        _launcherData = DownloadJson<LauncherData>("https://www.xtracube.dev/assets/js/launcherData.json");

        await Console.Out.WriteLineAsync(_launcherData.ToString());
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
            foreach (var process in processes)
            {
                process.Close();
            }
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
        if (path != null && Path.Exists(Path.GetFullPath(Path.Combine(path,"Among Us.exe"))))
        {
            _amongUsPath = path;
            LoadAmongUsPath();
            return;
        }
        Console.Out.WriteLine(Path.GetFullPath(Path.Combine(path, "Among Us.exe")));
        ButtonState = ButtonState.Refresh;
    }

    
    // have to use GetFullPath because path.combine is weird and uses wrong slashes sometimes
    public void LoadAmongUsPath()
    {
        Console.Out.WriteLine(Path.GetFullPath("Among Us.exe",_amongUsPath));

        var bepInPluginPath = Path.GetFullPath(Path.Combine(_amongUsPath, "BepInEx", "plugins"));

        if (!Path.Exists(Path.GetFullPath("Among Us.exe",_amongUsPath)))
        {
            Console.Out.WriteLine("no amogus detected");
            ButtonState = ButtonState.Refresh;
            return;
        }

        foreach (var process in Process.GetProcessesByName("Among Us"))
        {
            process.Kill();
        }
        
        if (Path.Exists(bepInPluginPath))
        {
            var filesPresent = true;
            var updateRequired = false;
            
            foreach (var info in _launcherData.ModList)
            {
                var pluginPath = Path.GetFullPath(Path.Combine(bepInPluginPath, info.Name));

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

        ProgressBar.ProgressTextFormat = "";
    }
    
    public async void InstallClickHandler(object sender, RoutedEventArgs args)
    {
        ProgressBar.Value = 0;
        switch (ButtonState)
        {
            case ButtonState.Update:
            case ButtonState.Install:
                await DownloadZip("BepInEx.zip",_amongUsPath,_launcherData.BepInEx);
                await InstallPlugins();
                await DownloadZip("ExtraData.zip",_amongUsPath,_launcherData.ExtraData);
                LoadAmongUsPath();
                break;
            
            case ButtonState.Launch:
                Launch();
                break;
            
            case ButtonState.Refresh:
                LocateAmongUs();
                break;

            case ButtonState.Running:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
        
    }

    private async Task DownloadZip(string name, string directory, LauncherData.ZipData zipData)
    {
        ProgressBar.ProgressTextFormat = $"Downloading {name}...";
        ProgressBar.Value = 0;
        var path = Path.GetFullPath(name, directory);

        if (Path.Exists(path) && FileToHash(path).Equals(zipData.Hash, StringComparison.OrdinalIgnoreCase))
        {
            await Console.Out.WriteLineAsync("exists");
        }
        else
        {
            await DownloadFile(name, directory, zipData.Link);
        }

        using var archive = ZipFile.Read(path);
        archive.ExtractProgress += (sender, args) =>
        {
            if (args.EntriesTotal != 0)
            {
                ProgressBar.Value = 100 * ((float)args.EntriesExtracted / args.EntriesTotal);
            }
        };
        archive.ExtractAll(_amongUsPath, ExtractExistingFileAction.OverwriteSilently);
        ProgressBar.ProgressTextFormat = "Done";
    }

    private async Task InstallPlugins()
    {
        var pluginPath = Path.GetFullPath(Path.Combine(_amongUsPath, "BepInEx", "plugins"));
        
        ProgressBar.ProgressTextFormat = "Installing mod...";
        ProgressBar.Value = 0;
        
        foreach (var plugin in _launcherData.ModList)
        {
            var path = Path.GetFullPath(plugin.Name, pluginPath);
            if (Path.Exists(path) && FileToHash(path).Equals(plugin.Hash, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            await DownloadFile(plugin.Name, pluginPath, plugin.Download);
        }
        
    }

    private void Launch()
    {
        ButtonState = ButtonState.Running;
        
        var process = Process.Start(Path.GetFullPath(Path.Combine(_amongUsPath,"Among Us.exe"))); 
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
        
        return FileToHash(path).Equals(hash, StringComparison.OrdinalIgnoreCase);
    }

    private static string FileToHash(string path)
    {
        var array = SHA256.HashData(File.ReadAllBytes(path));
        return string.Concat(array.Select(x => x.ToString("x2")));
    }

    private async void OpenDirectoryPicker(object? sender, RoutedEventArgs e)
    {
        var picked = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false
        });

        if (picked.Count > 0)
        {
            UpdateFromPath(picked[0].Path.LocalPath);
        }

    }

    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        var clipboard = GetTopLevel(this)?.Clipboard;
        var dataObject = new DataObject();
        dataObject.Set(DataFormats.Text, "https://dsc.gg/allofus");
        await clipboard.SetDataObjectAsync(dataObject);
    }
}