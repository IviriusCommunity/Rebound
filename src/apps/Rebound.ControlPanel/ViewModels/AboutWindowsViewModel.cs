// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core;
using Rebound.Core.Settings;
using Rebound.Core.SystemInformation.Hardware;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI.Localizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Rebound.ControlPanel.ViewModels;

internal partial class AboutWindowsViewModel : ObservableObject
{
    public readonly SettingsListener _listener;

    [ObservableProperty] public partial bool ShowCustomBranding { get; set; } = false;

    public AboutWindowsViewModel()
    {
        _listener = new SettingsListener();
    }

    // Hardware information
    [ObservableProperty] public partial string CpuName { get; set; } = "Loading...";
    [ObservableProperty] public partial string CpuArchitecture { get; set; } = "Loading...";

    [ObservableProperty] public partial string GpuName { get; set; } = "Loading...";

    [ObservableProperty] public partial long InstalledRam { get; set; } = 0;
    [ObservableProperty] public partial long UsableRam { get; set; } = 0;

    [ObservableProperty] public partial long PagefileSize { get; set; } = 0;

    [ObservableProperty] public partial double WindowsOccupiedSpace { get; set; } = 0;
    [ObservableProperty] public partial double TotalOccupiedSpace { get; set; } = 0;
    [ObservableProperty] public partial string WindowsOccupiedSpaceString { get; set; } = "Loading...";
    [ObservableProperty] public partial string TotalOccupiedSpaceString { get; set; } = "Loading...";

    [ObservableProperty] public partial string DeviceManufacturer { get; set; } = "Loading...";
    [ObservableProperty] public partial string DeviceModel { get; set; } = "Loading...";
    [ObservableProperty] public partial string MotherboardModel { get; set; } = "Loading...";

    // Software information
    [ObservableProperty] public partial string WindowsActivationInfo { get; set; } = "Loading...";
    [ObservableProperty] public partial string OSDisplayName { get; set; } = "Loading...";
    [ObservableProperty] public partial string DetailedWindowsVersion { get; set; } = "Loading...";
    [ObservableProperty] public partial string OSName { get; set; } = "Loading...";
    [ObservableProperty] public partial string ComputerName { get; set; } = "Loading...";

    // Init

    public void InitializePrimarySoftware()
    {
        OSDisplayName = WindowsInformation.GetOSDisplayName();
        DetailedWindowsVersion = WindowsInformation.GetDetailedWindowsVersion();
        OSName = WindowsInformation.GetOSName();
        ComputerName = WindowsInformation.GetComputerName();
    }

    public void InitializePrimaryHardware()
    {
        InstalledRam = RAM.GetInstalledRam();
        UsableRam = RAM.GetUsableRam();
    }
}
