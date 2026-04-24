// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core.SystemInformation.Hardware;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.UI;
using Rebound.Core.UI.Threading;

namespace Rebound.ControlPanel.ViewModels;

internal partial class HomeViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string WindowsVersionTitle { get; set; } = "Loading...";

    [ObservableProperty]
    public partial string CpuName { get; set; } = "Loading...";

    [ObservableProperty]
    public partial string GpuName { get; set; } = "Loading...";

    [ObservableProperty]
    public partial long RamCapacity { get; set; } = 0;

    [ObservableProperty]
    public partial string ComputerName { get; set; } = "Loading...";

    [ObservableProperty]
    public partial string Username { get; set; } = "Loading...";

    public HomeViewModel()
    {
        UIThread.QueueAction(() =>
        {
            WindowsVersionTitle = WindowsInformation.GetOSName();
            CpuName = CPU.GetName();
            GpuName = GPU.GetName();
            RamCapacity = RAM.GetInstalledRam();
            ComputerName = WindowsInformation.GetComputerName();
            Username = UserInformation.GetDisplayName();
        });
    }
}