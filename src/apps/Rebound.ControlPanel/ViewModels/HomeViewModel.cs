// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Rebound.Core.SystemInformation.Hardware;
using Rebound.Core.SystemInformation.Software;
using System.Threading.Tasks;

namespace Rebound.ControlPanel.ViewModels;

internal partial class HomeViewModel : ObservableObject
{
    [ObservableProperty] public partial string WindowsVersionTitle { get; set; } = "Loading...";

    [ObservableProperty] public partial string CpuName { get; set; } = "Loading...";

    [ObservableProperty] public partial string GpuName { get; set; } = "Loading...";

    [ObservableProperty] public partial long RamCapacity { get; set; } = 0;

    [ObservableProperty] public partial string ComputerName { get; set; } = "Loading...";

    [ObservableProperty] public partial string Username { get; set; } = "Loading...";

    [ObservableProperty] public partial bool IsReboundHubInstalled { get; set; }

    [ObservableProperty] public partial bool IsReboundShellInstalled { get; set; }

    [ObservableProperty] public partial bool IsWintoysInstalled { get; set; }

    [ObservableProperty] public partial bool IsPowerToysInstalled { get; set; }

    [ObservableProperty] public partial bool HasReboundApps { get; set; }

    [ObservableProperty] public partial bool Has3rdPartyApps { get; set; }

    public async Task InitializeAsync()
    {
        var result = await Task.Run(() =>
        {
            var osName = WindowsInformation.GetOSName();
            var cpuName = CPU.GetName();
            var gpuName = GPU.GetName();
            var ramCapacity = RAM.GetInstalledRam();
            var computerName = WindowsInformation.GetComputerName();
            var username = UserInformation.GetDisplayName();

            return (osName, cpuName, gpuName, ramCapacity, computerName, username);
        }).ConfigureAwait(true);
        WindowsVersionTitle = result.osName;
        CpuName = result.cpuName;
        GpuName = result.gpuName;
        RamCapacity = result.ramCapacity;
        ComputerName = result.computerName;
        Username = result.username;
    }
}