// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
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

namespace Rebound.About;

[ReboundApp("Rebound.About", "Legacy winver*legacy*ms-appx:///Assets/Exe.ico")]
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
            MainAppWindow = new MainWindow();
            MainAppWindow.Activate();
        }
        else
        {
            MainAppWindow.BringToFront();
        }

        InitializeLocalizer();
    }

    private async Task InitializeLocalizer()
    {

        // Initialize a "Strings" folder in the "LocalFolder" for the packaged app.
        StorageFolder localFolder = ApplicationData.Current.LocalFolder;
        StorageFolder stringsFolder = await localFolder.CreateFolderAsync(
          "Strings",
           CreationCollisionOption.OpenIfExists);

        // Create string resources file from app resources if doesn't exists.
        string resourceFileName = "Resources.resw";
        await CreateStringResourceFileIfNotExists(stringsFolder, "en-US", resourceFileName);
        await CreateStringResourceFileIfNotExists(stringsFolder, "es-ES", resourceFileName);
        await CreateStringResourceFileIfNotExists(stringsFolder, "ja", resourceFileName);
        await CreateStringResourceFileIfNotExists(stringsFolder, "nl", resourceFileName);


        ILocalizer localizer = await new LocalizerBuilder()
            .AddStringResourcesFolderForLanguageDictionaries(stringsFolder.Path)
            .SetOptions(options =>
            {
                options.DefaultLanguage = "en-US";
            })
            .Build();
    }

    private static async Task CreateStringResourceFileIfNotExists(StorageFolder stringsFolder, string language, string resourceFileName)
    {
        StorageFolder languageFolder = await stringsFolder.CreateFolderAsync(
            language,
            CreationCollisionOption.OpenIfExists);

        if (await languageFolder.TryGetItemAsync(resourceFileName) is null)
        {
            string resourceFilePath = Path.Combine(stringsFolder.Name, language, resourceFileName);
            StorageFile resourceFile = await LoadStringResourcesFileFromAppResource(resourceFilePath);
            _ = await resourceFile.CopyAsync(languageFolder);
        }
    }

    private static async Task<StorageFile> LoadStringResourcesFileFromAppResource(string filePath)
    {
        Uri resourcesFileUri = new($"ms-appx:///{filePath}");
        return await StorageFile.GetFileFromApplicationUriAsync(resourcesFileUri);
    }
}