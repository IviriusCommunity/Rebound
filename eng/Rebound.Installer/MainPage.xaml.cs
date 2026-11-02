// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Rebound.Forge;
using Rebound.Forge.Mods;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;

namespace Rebound.Installer;

internal sealed partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            await Catalog.ReboundHub.UpdateIntegrityAsync().ConfigureAwait(true);
        };
    }

    private static void InstallCertificate(string certificatePath)
    {
        using var certificate = X509CertificateLoader.LoadCertificateFromFile(certificatePath);

        using var store = new X509Store(
            StoreName.TrustedPeople,
            StoreLocation.LocalMachine);

        store.Open(OpenFlags.ReadWrite);

        store.Add(certificate);
    }

    [RelayCommand]
    public static void Cancel()
        => App.MainWindow?.Close();

    [RelayCommand]
    public async Task InstallAsync(CancellationToken cancellationToken)
    {
        App.CanClose = false;

        try
        {
            if (LegacyReboundRemover.OldReboundExists())
            {
                var dialog = new ContentDialog
                {
                    Title = "Old Rebound Installation Detected",
                    Content = "An older version of Rebound has been detected on your system. It is required to remove the old installation before proceeding with the new installation. Do you want to proceed with removing the old version of Rebound and installing the new one?",
                    PrimaryButtonText = "Proceed",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result != ContentDialogResult.Primary)
                    return;

                var progressDialog = new ContentDialog
                {
                    Title = "Deleting old Rebound",
                    Content = "This might take a while...",
                    XamlRoot = XamlRoot
                };

                var progressTask = progressDialog.ShowAsync().AsTask();

                try
                {
                    await Task.Run(
                        LegacyReboundRemover.DeleteOldRebound,
                        cancellationToken).ConfigureAwait(true);
                }
                finally
                {
                    progressDialog.Hide();
                    await progressTask.ConfigureAwait(true);
                }
            }

            InstallCertificate(
               Path.Combine(
                    AppContext.BaseDirectory,
                    "Hub",
                    "Rebound.Hub.cer"));
            await Catalog.ReboundHub.InstallAsync(cancellationToken)
                .ConfigureAwait(true);

            var (Installed, Intact) = await Catalog.ReboundHub
                .UpdateIntegrityAsync()
                .ConfigureAwait(true);

            if (Installed)
            {
                var finishDialog = new ContentDialog
                {
                    Title = "Rebound Hub installed",
                    Content = "Rebound Hub has been successfully installed!",
                    PrimaryButtonText = "Launch",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = XamlRoot
                };

                await finishDialog.ShowAsync();
                await Launcher.LaunchUriAsync(new("rebound-hub:"));
            }
            else
            {
                var finishDialog = new ContentDialog
                {
                    Title = "Rebound Hub installation failed",
                    Content = "Rebound Hub installation failed.",
                    CloseButtonText = "Close",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = XamlRoot
                };

                await finishDialog.ShowAsync();
            }
        }
        finally
        {
            App.CanClose = true;
            App.MainWindow?.Close();
        }
    }
}