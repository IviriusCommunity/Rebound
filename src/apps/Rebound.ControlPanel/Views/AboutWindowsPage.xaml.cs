// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Rebound.ControlPanel.ViewModels;
using Rebound.Core.Settings;
using Rebound.Core.SystemInformation.Software;
using System.Threading.Tasks;

namespace Rebound.ControlPanel.Views;

internal sealed partial class AboutWindowsPage : Page
{
    [GeneratedDependencyProperty(DefaultValue = InfoBarSeverity.Informational)] public partial InfoBarSeverity WindowsActivationSeverity { get; set; }

    [GeneratedDependencyProperty(DefaultValue = "ms-appx:///")] public partial string? CustomLogoPath { get; set; }

    AboutWindowsViewModel ViewModel { get; } = new();

    private readonly Microsoft.UI.Dispatching.DispatcherQueue _dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

    public AboutWindowsPage()
    {
        InitializeComponent();
        Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainPage_Loaded;

        // Fast init
        ViewModel.InitializePrimarySoftware();
        ViewModel.InitializePrimaryHardware();

        // All heavy work off UI thread
        _ = LoadDeferredAsync();

        ViewModel._listener.SettingChanged += (s, e) =>
        {
            _dispatcherQueue.EnqueueAsync(() =>
            {
                UpdateCustomBranding();
            });
        };

        UpdateCustomBranding();
    }

    private void UpdateCustomBranding()
    {
        if (SettingsManager.GetValue<string?>("CustomBrandingPath", "rebound") is string path && !string.IsNullOrWhiteSpace(path))
        {
            CustomLogoPath = path;
            ViewModel.ShowCustomBranding = true;
        }
        else
        {
            CustomLogoPath = "ms-appx:///Assets/ControlPanel.ico";
            ViewModel.ShowCustomBranding = false;
        }
    }

    private async Task LoadDeferredAsync()
    {
        // Capture everything on background thread
        var results = await Task.Run(() =>
        {
            var activationType = WindowsInformation.GetWindowsActivationType();
            var activationInfo = activationType switch
            {
                WindowsActivationType.Unlicensed => "Unlicensed",
                WindowsActivationType.Activated => "Activated",
                WindowsActivationType.GracePeriod => "Grace period",
                WindowsActivationType.NonGenuine => "Non-genuine",
                WindowsActivationType.ExtendedGracePeriod => "Extended grace period",
                _ => "Unknown"
            };

            // Activation severity
            var severity = activationType switch
            {
                WindowsActivationType.Unlicensed => InfoBarSeverity.Error,
                WindowsActivationType.Activated => InfoBarSeverity.Success,
                WindowsActivationType.GracePeriod => InfoBarSeverity.Warning,
                WindowsActivationType.NonGenuine => InfoBarSeverity.Error,
                WindowsActivationType.ExtendedGracePeriod => InfoBarSeverity.Warning,
                _ => InfoBarSeverity.Informational
            };

            return new
            {
                activationInfo,
                severity
            };
        }).ConfigureAwait(true);

        // Marshal all results back to UI thread in one shot
        ViewModel.WindowsActivationInfo = results.activationInfo;
        WindowsActivationSeverity = results.severity;
    }
}