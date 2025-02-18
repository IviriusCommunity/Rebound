using H.NotifyIcon.Core;
using Microsoft.UI.Xaml;
using Rebound.Generators;
using Rebound.Helpers.Services;
using WinUIEx;

#nullable enable

namespace Rebound.ShellExperienceHost;

[ReboundApp("Rebound.ShellExperienceHost", "Legacy Shell")]
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    private void OnSingleInstanceLaunched(object? sender, SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            var icon = new TrayIcon();
        }
    }

    public static WindowEx? m_window;
}