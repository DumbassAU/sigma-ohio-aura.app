using Avalonia.Controls;

namespace AOULauncher.Views;

public partial class Error : Window
{
    public Error()
    {
        InitializeComponent();
        ErrorText.Text = "An error occurred.";
    }

    public Error(string error)
    {
        InitializeComponent();
        ErrorText.Text = error;
    }
}