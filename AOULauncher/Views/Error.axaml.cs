using Avalonia.Controls;

namespace AOULauncher.Views;

public partial class Error : Window
{
    public Error(string error)
    {
        InitializeComponent();
        ErrorText.Text = error;
    }
}