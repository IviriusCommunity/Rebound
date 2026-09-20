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

internal sealed partial class PrivacyAndUserChoicePage : Page
{
    private PrivacyAndUserChoiceViewModel ViewModel { get; } = new();

    public PrivacyAndUserChoicePage()
    {
        InitializeComponent();
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
                Arguments = CplArgs.PRIVACY_USER_CHOICE
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