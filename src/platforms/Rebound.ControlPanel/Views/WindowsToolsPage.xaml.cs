using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;

namespace Rebound.ControlPanel.Views;

public sealed partial class WindowsToolsPage : Page
{
    public WindowsToolsPage()
    {
        InitializeComponent();
    }

    [RelayCommand]
    private void LaunchApp(string name)
    {
        if (name == "taskmgr")
        {
            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "taskmgr",
                    UseShellExecute = true,
                    Verb = "runas"
                });
            }
            catch
            {

            }
        }
        else
        {
            Process.Start(name);
        }
    }
}
