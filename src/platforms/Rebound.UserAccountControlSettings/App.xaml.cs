using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Rebound.Forge;
using Rebound.Generators;
using Rebound.Helpers.AppEnvironment;

namespace Rebound.UserAccountControlSettings;

[ReboundApp("Rebound.UACSettings", "Legacy User Account Control Settings*legacy*ms-appx:///Assets/ActionCenterLegacy.ico")]
public partial class App : Application
{
    private async void OnSingleInstanceLaunched(object? sender, Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        if (e.Arguments == "legacy")
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
            await IFEOEngine.PauseIFEOEntryAsync("useraccountcontrolsettings.exe").ConfigureAwait(true);
            Process.Start(new ProcessStartInfo
            {
                FileName = "useraccountcontrolsettings.exe",
                UseShellExecute = true
            });
            await IFEOEngine.ResumeIFEOEntryAsync("useraccountcontrolsettings.exe").ConfigureAwait(true);
            Process.GetCurrentProcess().Kill();
            return;
        }

        if (e.IsFirstLaunch)
        {
            MainAppWindow = new MainWindow();
            MainAppWindow.Activate();
        }
        else
        {
            MainAppWindow.BringToFront();
        }
    }
}