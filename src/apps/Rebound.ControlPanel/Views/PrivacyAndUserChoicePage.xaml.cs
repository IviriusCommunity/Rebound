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
        Loaded += async (s, e) =>
        {
            await ViewModel.FeedbackHub.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.GetHelp.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.MicrosoftStore.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.Notepad.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.Paint.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.People.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.PhoneLink.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.SnippingTool.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.Terminal.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.WindowsWebExperiencePack.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.XboxGameBar.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.Bing.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.Calculator.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.Camera.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.Clipchamp.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.Clock.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.Copilot.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.MediaPlayer.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.Microsoft365Copilot.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.MicrosoftSolitaireCollection.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.News.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.Photos.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.SoundRecorder.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.ToDo.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.Weather.UpdateIntegrityAsync().ConfigureAwait(false);
            await ViewModel.Xbox.UpdateIntegrityAsync().ConfigureAwait(false);
        };
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