using System;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Xaml;
using Rebound.Core.Helpers;
using Rebound.Generators;

namespace Rebound.Keyboard;

[ReboundApp("Rebound.OSK", "Legacy On-Screen Keyboard*legacy*ms-appx:///On-Screen Keyboard.ico")]
public partial class App : Application
{
    public static ReboundPipeClient ReboundPipeClient { get; set; }

    private async void OnSingleInstanceLaunched(object? sender, Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            ReboundPipeClient = new ReboundPipeClient();
            await ReboundPipeClient.ConnectAsync();
        }

        if (!Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "osk.exe").ArgsMatchKnownEntries([string.Empty], e.Arguments))
        {
            await ReboundPipeClient.SendMessageAsync("IFEOEngine::Pause#osk.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = "osk.exe",
                UseShellExecute = true,
                Arguments = e.Arguments == "legacy" ? string.Empty : e.Arguments
            });
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