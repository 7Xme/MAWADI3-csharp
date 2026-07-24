using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Mawadi3Print.ViewModels;

namespace Mawadi3Print;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            // Settings dialog auto-opens if no key (handled in ViewModel)
        }
    }

    private void Hyperlink_OpenApiKey(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Clipboard.SetText(e.Uri.ToString());
        }
        catch { }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.ToString(),
                UseShellExecute = true
            });
        }
        catch { }

        e.Handled = true;
    }
}