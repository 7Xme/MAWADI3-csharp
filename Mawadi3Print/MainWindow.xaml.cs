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
        DataContext = new MainViewModel();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            try
            {
                await vm.InitializeAsync();

                if (!vm.HasApiKey)
                {
                    await Task.Delay(300);
                    await vm.OpenSettingsCommand.ExecuteAsync(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "خطأ في التهيئة", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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