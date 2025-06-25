using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.ControlPanel.Views;
using Rebound.Forge;
using Rebound.Generators;
using Rebound.Helpers.AppEnvironment;

namespace Rebound.ControlPanel;

[ReboundApp("Rebound.Control", "Legacy Control Panel*legacy*ms-appx:///Assets/ControlPanelLegacy.ico")]
public partial class App : Application
{
    private async void OnSingleInstanceLaunched(object? sender, Rebound.Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        Type? pageToLaunch = null;

        if (e.Arguments is "admintools" or "/name Microsoft.AdministrativeTools")
        {
            pageToLaunch = typeof(WindowsToolsPage);
        }
        else if (!string.IsNullOrEmpty(e.Arguments))
        {
            if (!this.IsRunningAsAdmin())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Environment.ProcessPath,
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = "legacy"
                });
                Process.GetCurrentProcess().Kill();
                return;
            }
            await IFEOEngine.PauseIFEOEntryAsync("control.exe").ConfigureAwait(true);
            Process.Start(new ProcessStartInfo
            {
                FileName = "control.exe",
                UseShellExecute = true
            });
            await IFEOEngine.ResumeIFEOEntryAsync("control.exe").ConfigureAwait(true);
            Process.GetCurrentProcess().Kill();
            return;
        }

        if (e.IsFirstLaunch)
        {
            MainAppWindow = new MainWindow();
            MainAppWindow.Activate();
            if (pageToLaunch != null)
            {
                _ = ((((MainAppWindow as MainWindow).Content as Frame)?.Content as RootPage)?.RootFrame.Navigate(pageToLaunch));
            }
        }
        else
        {
            MainAppWindow.BringToFront();
        }
    }
}