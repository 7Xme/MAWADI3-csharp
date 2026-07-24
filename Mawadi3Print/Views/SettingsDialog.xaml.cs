using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace Mawadi3Print.Views;

public partial class SettingsDialog
{
    public SettingsDialog()
    {
        InitializeComponent();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
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