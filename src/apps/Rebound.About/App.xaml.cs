// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.UI.Xaml;
using Rebound.Core.Helpers;
using Rebound.Generators;
using Rebound.Helpers;
using Rebound.Helpers.Services;
using Windows.Storage;
using Windows.System.UserProfile;
using WinUI3Localizer;
using WinUIEx;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1515 // Consider making public types internal

namespace Rebound.About;

[ReboundApp("Rebound.About", "Legacy winver*legacy*ms-appx:///Assets/Exe.ico")]
public partial class App : Application
{
    public static ReboundPipeClient ReboundPipeClient { get; set; }

    public static ILocalizer Localizer { get; set; }

    private async void OnSingleInstanceLaunched(object? sender, SingleInstanceLaunchEventArgs e)
    {
        if (e.IsFirstLaunch)
        {
            ReboundPipeClient = new ReboundPipeClient();
            await ReboundPipeClient.ConnectAsync();
        }

        if (e.Arguments == "legacy")
        {
            await ReboundPipeClient.SendMessageAsync("IFEOEngine::Pause#winver.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = "winver.exe",
                UseShellExecute = true,
            });
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

            if (SettingsHelper.GetValue("FetchMode", "rebound", false))
            {
                MainAppWindow.SetWindowSize(850, 480);
            }
            else
            {
                MainAppWindow.Width = SettingsHelper.GetValue("IsSidebarOn", "winver", false) ? 720 : 520;
                MainAppWindow.Height = SettingsHelper.GetValue("IsReboundOn", "winver", true) ? 640 : 500;
            }
            MainAppWindow.Activate();
        }
        else
        {
            MainAppWindow.BringToFront();
        }
    }
}