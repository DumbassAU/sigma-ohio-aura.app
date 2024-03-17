using System;
using System.IO;
using Avalonia.Controls;
using Gameloop.Vdf;

namespace AOULauncher.Views;

public partial class MainWindow : Window
{
    private const string SteamLibraryFilePath = @"C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf";
    private const string AmongUsSteamSuffix = @"\steamapps\common\Among Us";

    private string _amongUsPath = "";
    
    public MainWindow()
    {
        InitializeComponent();

        if (File.Exists(SteamLibraryFilePath))
        {
            dynamic libraries = VdfConvert.Deserialize(File.ReadAllText(SteamLibraryFilePath));

            
            foreach (var val in libraries.Value)
            {
                foreach (var app in val.Value.apps)
                {
                    if (app.Key == "945360")
                    {
                        _amongUsPath = val.Value.path + AmongUsSteamSuffix;
                    }
                }
            }
            
            Console.Out.WriteLine(_amongUsPath);


        }
        else
        {
            
        }
        
    }
}