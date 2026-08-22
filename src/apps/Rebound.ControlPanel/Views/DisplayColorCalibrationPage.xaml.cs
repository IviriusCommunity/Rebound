// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using Rebound.ControlPanel.Brushes;
using Rebound.ControlPanel.ViewModels;
using Rebound.Core.ICC.Profiles;
using Rebound.Core.SystemInformation.Hardware;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Rebound.ControlPanel.Views;

internal sealed partial class DisplayColorCalibrationPage : Page
{
    [GeneratedDependencyProperty] private partial SDRCalibrationBackdropBrush? OriginalBrush { get; set; }

    [GeneratedDependencyProperty] private partial SDRCalibrationBackdropBrush? ModifiedBrush { get; set; }

    private DisplayColorCalibrationViewModel ViewModel { get; }

    private Action? _closeWindowAction;

    private int _previousPage;

    public DisplayColorCalibrationPage()
    {
        // Create brushes
        OriginalBrush = new();
        ModifiedBrush = new();

        // Parse window closing action
        ViewModel = new();

        ViewModel.PropertyChanged += (s, e) =>
        {
            switch (e.PropertyName)
            {
                // Handle page logic
                case nameof(ViewModel.SelectedPageIndex):
                    {
                        switch (ViewModel.SelectedPageIndex)
                        {
                            // First page: windowed, no back button
                            case 0:
                                {
                                    unsafe
                                    {
                                        // Make overlapped
                                        var appWindow = AppWindow.GetFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
                                        var handle = Win32Interop.GetWindowFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
                                        appWindow.SetPresenter(OverlappedPresenter.Create());

                                        // Center on screen
                                        var scale = Display.GetScale(new((void*)Win32Interop.GetWindowFromWindowId(appWindow.Id)));
                                        appWindow.Resize(new((int)(800 * scale), (int)(600 * scale)));
                                        WinUIEx.HwndExtensions.CenterOnScreen(handle);
                                    }
                                    ViewModel.CanNavigateBack = false;
                                    break;
                                }
                            // Second page: full-screen, back button
                            case 1:
                                {
                                    // If the previous page is not the first, skip initializing
                                    if (_previousPage != 0)
                                        break;

                                    // Dispose of the old brushes
                                    OriginalBrush?.Dispose();
                                    ModifiedBrush?.Dispose();

                                    // Make new ones
                                    OriginalBrush = new();
                                    ModifiedBrush = new();

                                    // Query the current color profile
                                    OriginalBrush.ReloadCurrentColorProfile();
                                    ModifiedBrush.ReloadCurrentColorProfile();

                                    // Set defaults
                                    ModifiedBrush.Gamma = 1;
                                    ModifiedBrush.RedGain = 1;
                                    ModifiedBrush.GreenGain = 1;
                                    ModifiedBrush.BlueGain = 1;

                                    // Make fullscreen
                                    AppWindow.GetFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId).SetPresenter(FullScreenPresenter.Create());
                                    ViewModel.CanNavigateBack = true;
                                    break;
                                }
                            default:
                                break;
                        }
                        _previousPage = ViewModel.SelectedPageIndex;
                        break;
                    }
                default:
                    break;
            }
        };
        InitializeComponent();
    }

    [RelayCommand]
    public async Task FinishAsync()
    {
        if (ViewModel.IsSoftwareCalibration)
        {
            var bytes = WcsProfile.GeneratePerChannel(
                ViewModel.Name,
                ViewModel.Description,
                ModifiedBrush!.Gamma,
                ModifiedBrush.Gamma,
                ModifiedBrush.Gamma,
                ModifiedBrush.RedGain,
                ModifiedBrush.GreenGain,
                ModifiedBrush.BlueGain);

            if (bytes == null) return;

            var picker = new FileSavePicker(XamlRoot.ContentIslandEnvironment.AppWindowId)
            {
                DefaultFileExtension = ".icc",
                SuggestedFileName = "New Calibration Profile",
                Title = "Save Calibration Profile",
                CommitButtonText = "Save Profile",
                SuggestedStartLocation = PickerLocationId.Desktop,
            };

            // Show the picker dialog
            var result = await picker.PickSaveFileAsync();

            if (result == null) return;

            string path = result.Path;
            await Task.Run(() =>
            {
                File.WriteAllBytes(path, bytes);
            }).ConfigureAwait(false);
        }

        Close();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _closeWindowAction = (Action)e.Parameter;
    }

    [RelayCommand]
    public void Close()
        => DispatcherQueue.TryEnqueue(_closeWindowAction!.Invoke);

    [RelayCommand]
    public void Begin(bool isSoftwareCalibration)
    {
        ViewModel.SelectedPageIndex++;
        ViewModel.IsSoftwareCalibration = isSoftwareCalibration;
    }

    [RelayCommand]
    public void NavigateBackward()
        => ViewModel.SelectedPageIndex--;

    [RelayCommand]
    public void NavigateForward()
        => ViewModel.SelectedPageIndex++;
}