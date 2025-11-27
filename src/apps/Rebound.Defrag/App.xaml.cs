using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Rebound.Forge;
using Rebound.Generators;
using Rebound.Helpers.AppEnvironment;

namespace Rebound.Defrag;

[ReboundApp("Rebound.Defrag", "Legacy Defragment and Optimize Drives*legacy*ms-appx:///Assets/dfrguiLegacy.ico")]
public partial class App : Application
{
    private async void OnSingleInstanceLaunched(object? sender, Rebound.Helpers.Services.SingleInstanceLaunchEventArgs e)
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
            await IFEOEngine.PauseIFEOEntryAsync("dfrgui.exe").ConfigureAwait(true);
            Process.Start(new ProcessStartInfo
            {
                FileName = "dfrgui.exe",
                UseShellExecute = true
            });
            await IFEOEngine.ResumeIFEOEntryAsync("dfrgui.exe").ConfigureAwait(true);
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