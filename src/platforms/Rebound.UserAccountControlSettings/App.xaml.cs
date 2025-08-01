using System;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Xaml;
using Rebound.Core.Helpers;
using Rebound.Generators;

namespace Rebound.UserAccountControlSettings;

[ReboundApp("Rebound.UACSettings", "Legacy User Account Control Settings*legacy*ms-appx:///Assets/ActionCenterLegacy.ico")]
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

        if (!Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "useraccountcontrolsettings.exe").ArgsMatchKnownEntries([string.Empty], e.Arguments))
        {
            await ReboundPipeClient.SendMessageAsync("IFEOEngine::Pause#useraccountcontrolsettings.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = "useraccountcontrolsettings.exe",
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