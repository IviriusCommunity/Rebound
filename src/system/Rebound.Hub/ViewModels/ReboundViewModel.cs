// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rebound.Core;
using Rebound.Core.Helpers;
using Rebound.Core.UI;
using Rebound.Forge;
using Rebound.Hub.Cards;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using Windows.ApplicationModel;
using Windows.System;

namespace Rebound.Hub.ViewModels;

internal partial class ReboundViewModel : ObservableObject
{
    public ObservableCollection<LinkCard> LinkCards { get; } =
    [
        new LinkCard
        {
            Title = "Get Started",
            Description = "See a short tutorial on how to use Rebound.",
            IconPath = "/Assets/Glyphs/DesktopVerify.ico",
            Link = "https://www.youtube.com/watch?v=tJ8AnfZP4EU"
        },
        new LinkCard
        {
            Title = "WinUI apps",
            Description = "Rebound uses only WinUI apps to ensure a consistent experience.",
            IconPath = "/Assets/Glyphs/WinUI.png",
            Link = "https://learn.microsoft.com/en-us/windows/apps/winui/winui3/"
        },
        new LinkCard
        {
            Title = "Windows updates",
            Description = "Rebound does not disable Windows updates so you can enjoy fresh patches and releases.",
            IconPath = "/Assets/Glyphs/Update.ico",
            Link = "https://support.microsoft.com/en-us/windows/install-windows-updates-3c5ae7fc-9fb6-9af1-1984-b5e0412c556a"
        },
        new LinkCard
        {
            Title = "Rebound updates",
            Description = "All Rebound updates are easy to install via the \"Update or Repair all\" option.",
            IconPath = "/Assets/Glyphs/Restart.ico",
            Link = "https://ivirius.com/rebound"
        },
        new LinkCard
        {
            Title = "GitHub",
            Description = "Star the repo and contribute to the project!",
            IconPath = "/Assets/Glyphs/GitHub.png",
            Link = "https://github.com/IviriusCommunity/Rebound"
        }
    ];

    [ObservableProperty] public partial bool IsReboundEnabled { get; set; }
    [ObservableProperty] public partial bool IsUpdateAvailable { get; set; }
    [ObservableProperty] public partial string VersionText { get; set; } = "";
    [ObservableProperty] public partial string CurrentVersion { get; set; } = "";
    [ObservableProperty] public partial bool HasSideloadedMods { get; set; }

    public ReboundViewModel()
    {
        LoadSettings();
        CheckForUpdates();
        _ = InitializeAsync();
    }

    private async void LoadSettings()
    {
        bool enabled = true;
        foreach (var mod in Catalog.MandatoryMods)
        {
            var props = await mod.UpdateIntegrityAsync().ConfigureAwait(false);
            if (!props.Installed || !props.Intact)
            {
                ReboundLogger.Log($"[ReboundViewModel] Enabled changed for mod {mod.Name}: False");
                enabled = false;
            }
            ReboundLogger.Log($"[ReboundViewModel] Integrity check for {mod.Name}: IsInstalled={props.Installed}, IsIntact={props.Intact}");
        }
        ReboundLogger.Log($"[ReboundViewModel] Enabled (original thread): {enabled}");
        UIThreadQueue.QueueAction(async () =>
        {
            ReboundLogger.Log($"[ReboundViewModel] Enabled: {enabled}");
            IsReboundEnabled = enabled;
            HasSideloadedMods = Catalog.SideloadedMods.Count > 0;
        });
    }

    private async Task InitializeAsync()
    {
        if (IsReboundEnabled)
        {
            foreach (var mod in Catalog.MandatoryMods)
            {
                if (!mod.IsIntact) await mod.RepairAsync().ConfigureAwait(false);
            }
            /*if (EnableAutomaticRepair)
            {
                foreach (var mod in Catalog.Mods)
                {
                    if (!mod.IsIntact) await mod.RepairAsync();
                }
            }*/
        }
    }

    public void CheckForUpdates()
    {
        try
        {
            var version = $"{Package.Current.GetAppInstallerInfo().Version.Major}.{Package.Current.GetAppInstallerInfo().Version.Minor}.{Package.Current.GetAppInstallerInfo().Version.Revision}.{Package.Current.GetAppInstallerInfo().Version.Build}";
            CurrentVersion = version;
            VersionText = $"Current version: {version}  -  New version: {Variables.ReboundVersion}";
            IsUpdateAvailable = Variables.ReboundVersion != version;
        }
        catch (Exception ex)
        {
            ReboundLogger.Log("[ReboundViewModel] Failed to check for updates.", ex);
            IsUpdateAvailable = false;
        }
    }

    [RelayCommand]
    public async Task UpdateOrRepairAllAsync()
    {
        var tasks = new List<Task>();

        tasks.AddRange(Catalog.Mods.Where(m => m.IsInstalled).Select(m => m.InstallAsync()));
        tasks.AddRange(Catalog.MandatoryMods.Select(m => m.InstallAsync()));

        await Task.WhenAll(tasks).ConfigureAwait(false);

        CheckForUpdates();
    }

    [RelayCommand]
    public async Task ViewLogFileAsync()
    {
        await Launcher.LaunchUriAsync(new(Variables.ReboundLogFile));
    }

    [RelayCommand]
    public void DeleteLogFile()
    {
        File.Delete(Variables.ReboundLogFile);
    }

    [RelayCommand]
    public async Task ToggleReboundAsync()
    {
        if (IsReboundEnabled)
        {
            await DisableReboundAsync();
            UIThreadQueue.QueueAction(async () =>
            {
                IsReboundEnabled = false;
            });
            return;
        }
        else
        {
            EnableRebound();
            UIThreadQueue.QueueAction(async () =>
            {
                IsReboundEnabled = true;
            });
            return;
        }
    }

    [RelayCommand]
    public async Task EnableRebound()
    {
        foreach (var mod in Catalog.MandatoryMods)
        {
            await mod.InstallAsync();
        }
    }

    [RelayCommand]
    public async Task DisableReboundAsync()
    {
        foreach (var mod in Catalog.Mods)
        {
            await mod.UninstallAsync();
        }
        foreach (var mod in Catalog.MandatoryMods)
        {
            await mod.UninstallAsync();
        }
    }
}