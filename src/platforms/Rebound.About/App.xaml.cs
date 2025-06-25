// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Rebound.Forge;
using Rebound.Generators;
using Rebound.Helpers.AppEnvironment;
using Windows.Storage;
using Windows.System.UserProfile;
using WinUI3Localizer;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1515 // Consider making public types internal

namespace Rebound.About;

[ReboundApp("Rebound.About", "Legacy winver*legacy*ms-appx:///Assets/Exe.ico")]
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
            await IFEOEngine.PauseIFEOEntryAsync("winver.exe").ConfigureAwait(true);
            Process.Start(new ProcessStartInfo
            {
                FileName = "winver.exe",
                UseShellExecute = true
            });
            await IFEOEngine.ResumeIFEOEntryAsync("winver.exe").ConfigureAwait(true);
            Process.GetCurrentProcess().Kill();
            return;
        }

        if (e.IsFirstLaunch)
        {
            var stringsFolderPath = Path.Combine(AppContext.BaseDirectory, "Strings");

            var stringsFolder = await StorageFolder.GetFolderFromPathAsync(stringsFolderPath);

            Localizer = new LocalizerBuilder()
                .AddStringResourcesFolderForLanguageDictionaries(stringsFolderPath)
                .SetOptions(options =>
                {
                    options.DefaultLanguage = "en-US";
                })
                .Build();

            var stringFolders = await stringsFolder.GetFoldersAsync(Windows.Storage.Search.CommonFolderQuery.DefaultQuery);

            if (stringFolders.Any(item =>
                item.Name.Equals(GlobalizationPreferences.Languages[0], StringComparison.OrdinalIgnoreCase)))
            {
                Localizer.SetLanguage(GlobalizationPreferences.Languages[0]);
            }
            else
            {
                Localizer.SetLanguage("en-US");
            }

            MainAppWindow = new MainWindow();
            MainAppWindow.Activate();
        }
        else
        {
            MainAppWindow.BringToFront();
        }
    }
}