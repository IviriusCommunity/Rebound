// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using Rebound.ControlPanel.ViewModels;
using Rebound.Core.UI;
using System;
using System.Threading.Tasks;

namespace Rebound.ControlPanel.Views;

internal sealed partial class UserAccountControlSettingsPage : Page
{
    private UserAccountControlSettingsViewModel ViewModel { get; } = new();

    public UserAccountControlSettingsPage()
    {
        InitializeComponent();
    }

    [RelayCommand]
    public async Task ApplyTemplateAsync(int index)
    {
        var cd = new ContentDialog()
        {
            Title = "Rebound Control Panel",
            Content = $"Are you sure you want to apply the following User Account Control template?",
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };
        var result = await cd.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            switch (index)
            {
                case 0: // Always notify
                    ViewModel.PromptOnSecureDesktop = true;
                    ViewModel.AllowSilentElevation = false;
                    ViewModel.ElevationPromptForStandardUsers = 2;
                    ViewModel.AdministratorsInAdminApprovalMode = 2;
                    break;

                case 1: // Linux-inspired
                    ViewModel.PromptOnSecureDesktop = false;
                    ViewModel.AllowSilentElevation = false;
                    ViewModel.ElevationPromptForStandardUsers = 1;
                    ViewModel.AdministratorsInAdminApprovalMode = 3;
                    break;

                case 2: // Windows default
                    ViewModel.PromptOnSecureDesktop = true;
                    ViewModel.AllowSilentElevation = false;
                    ViewModel.ElevationPromptForStandardUsers = 2;
                    ViewModel.AdministratorsInAdminApprovalMode = 5;
                    break;

                case 3: // Notify without secure desktop
                    ViewModel.PromptOnSecureDesktop = false;
                    ViewModel.AllowSilentElevation = false;
                    ViewModel.ElevationPromptForStandardUsers = 2;
                    ViewModel.AdministratorsInAdminApprovalMode = 5;
                    break;

                case 4: // Never notify
                    ViewModel.PromptOnSecureDesktop = false;
                    ViewModel.AllowSilentElevation = true;
                    ViewModel.ElevationPromptForStandardUsers = 2;
                    ViewModel.AdministratorsInAdminApprovalMode = 0;
                    break;
            }
        }
    }

    [RelayCommand]
    public async Task RelaunchAsAdminAsync()
    {
        try
        {
            App.SingleInstanceAppService.Relaunch(new InstanceRelaunchOptions
            {
                Elevated = true,
                ShutdownCurrent = true,
                ForceNewInstance = true,
                Arguments = CplArgs.UAC_SETTINGS
            });
        }
        catch (Exception ex)
        {
            await DispatcherQueue.EnqueueAsync(async () =>
            {
                var cd = new ContentDialog()
                {
                    Title = "Rebound Control Panel",
                    Content = $"Couldn't launch Rebound Control Panel as administrator.\n\n{ex.Message}",
                    CloseButtonText = "Ok",
                    XamlRoot = XamlRoot
                };
                await cd.ShowAsync();
            }).ConfigureAwait(false);
        }
    }
}
