﻿// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Rebound.Forge;
using Rebound.Generators;
using Rebound.Helpers.AppEnvironment;
using Windows.Storage;
using WinUI3Localizer;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1515 // Consider making public types internal

namespace Rebound.ManagementConsole;

[ReboundApp("Rebound.ManagementConsole", "Legacy MMC*legacy*ms-appx:///Assets/StoreLogo.png")]
public partial class App : Application
{
    public static ILocalizer Localizer { get; set; }

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
            await IFEOEngine.PauseIFEOEntryAsync("mmc.exe").ConfigureAwait(true);
            Process.Start(new ProcessStartInfo
            {
                FileName = "winver.exe",
                UseShellExecute = true
            });
            await IFEOEngine.ResumeIFEOEntryAsync("mmc.exe").ConfigureAwait(true);
            Process.GetCurrentProcess().Kill();
            return;
        }

        if (e.IsFirstLaunch)
        {
            /*var stringsFolderPath = Path.Combine(AppContext.BaseDirectory, "Strings");

            var stringsFolder = await StorageFolder.GetFolderFromPathAsync(stringsFolderPath);

            Localizer = new LocalizerBuilder()
                .AddStringResourcesFolderForLanguageDictionaries(stringsFolderPath)
                .SetOptions(options =>
                {
                    options.DefaultLanguage = "en-US";
                })
                .Build();*/

            MainAppWindow = new MainWindow();
            MainAppWindow.Activate();
        }
        else
        {
            MainAppWindow.BringToFront();
        }
    }
}