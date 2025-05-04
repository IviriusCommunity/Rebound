// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Rebound.Generators;
using Rebound.Helpers.AppEnvironment;
using Rebound.Forge;

namespace Rebound.Cleanup;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1515 // Consider making public types internal

[ReboundApp("Rebound.Cleanup", "Legacy Disk Cleanup*legacy*ms-appx:///Assets/cleanmgrLegacy.ico")]
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
            await IFEOEngine.PauseIFEOEntryAsync("cleanmgr.exe").ConfigureAwait(true);
            Process.Start(new ProcessStartInfo
            {
                FileName = "cleanmgr.exe",
                UseShellExecute = true
            });
            await IFEOEngine.ResumeIFEOEntryAsync("cleanmgr.exe").ConfigureAwait(true);
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