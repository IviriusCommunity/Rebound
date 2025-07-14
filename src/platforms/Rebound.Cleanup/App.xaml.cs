// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Rebound.Generators;
using Rebound.Helpers.AppEnvironment;
using Rebound.Forge;
using System.IO;
using Rebound.Core.Helpers;
using System.Collections.Generic;

namespace Rebound.Cleanup;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1515 // Consider making public types internal

[ReboundApp("Rebound.Cleanup", "Legacy Disk Cleanup*legacy*ms-appx:///Assets/cleanmgrLegacy.ico")]
public partial class App : Application
{
    public static ReboundPipeClient ReboundPipeClient { get; set; }

    private async void OnSingleInstanceLaunched(object? sender, Rebound.Helpers.Services.SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            ReboundPipeClient = new ReboundPipeClient();
            await ReboundPipeClient.ConnectAsync();
        }

        if (!Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cleanmgr.exe").ArgsMatchKnownEntries([string.Empty], e.Arguments))
        {
            await ReboundPipeClient.SendMessageAsync("IFEOEngine::Pause#cleanmgr.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = "cleanmgr.exe",
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