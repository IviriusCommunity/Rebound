// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rebound.Core;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Rebound.Forge;

public enum ModCategory
{
    General,
    Productivity,
    SystemAdministration,
    Customization,
    Extras,
    Sideloaded
}

public partial class Mod : ObservableObject
{
    [ObservableProperty] public partial bool IsInstalled { get; set; } = false;
    [ObservableProperty] public partial bool IsIntact { get; set; } = true;

    public string Name { get; }
    public string Description { get; }
    public string EntryExecutable { get; set; } = string.Empty;
    public string Icon { get; }
    public string InstallationSteps { get; }
    public ModCategory Category { get; }
    public InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Extras;
    public ObservableCollection<ICog> Instructions { get; }
    public ObservableCollection<IModItem>? Settings { get; set; } = new();
    public string ProcessName { get; }

    public Mod(
        string name,
        string description,
        string icon,
        string installationSteps,
        ObservableCollection<ICog> instructions,
        string processName,
        ModCategory category,
        ObservableCollection<IModItem>? settings = null)
    {
        Name = name;
        Description = description;
        Icon = icon;
        InstallationSteps = installationSteps;
        Instructions = instructions;
        ProcessName = processName;
        Settings = settings;
        Category = category;
    }

    [RelayCommand]
    public async Task InstallAsync()
    {
        try
        {
            ReboundLogger.Log($"[Mod] Installing {Name}...");

            // Kill any running processes first
            var runningProcesses = Process.GetProcessesByName(ProcessName).ToList();
            var pathsToRestart = runningProcesses
                .Select(p =>
                {
                    try { return p.MainModule?.FileName; }
                    catch { return null; }
                })
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .ToList();

            foreach (var process in runningProcesses)
            {
                try { process.Kill(); } catch { }
            }

            await Task.Delay(200);

            // Apply all instructions
            foreach (var instruction in Instructions)
                await instruction.ApplyAsync();

            // Update status
            await UpdateIntegrityAsync();

            // Restart processes if needed
            foreach (var path in pathsToRestart)
            {
                try { Process.Start(path); } catch { }
            }

            ReboundLogger.Log($"[Mod] Installation finished for {Name}");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log($"[Mod] InstallAsync failed for {Name}", ex);
        }
    }

    [RelayCommand]
    public void Open()
    {
        try
        {
            Process.Start(EntryExecutable);
        }
        catch (Exception ex)
        {
            ReboundLogger.Log($"[Mod] Open failed for {Name}", ex);
        }
    }

    [RelayCommand]
    public async Task UninstallAsync()
    {
        try
        {
            ReboundLogger.Log($"[Mod] Uninstalling {Name}...");

            foreach (var p in Process.GetProcessesByName(ProcessName))
            {
                try { p.Kill(); } catch { }
            }

            await Task.Delay(200);

            foreach (var instruction in Instructions)
                await instruction.RemoveAsync();

            await UpdateIntegrityAsync();

            ReboundLogger.Log($"[Mod] Uninstallation finished for {Name}");
        }
        catch (Exception ex)
        {
            ReboundLogger.Log($"[Mod] UninstallAsync failed for {Name}", ex);
        }
    }

    [RelayCommand]
    public async Task RepairAsync() => await InstallAsync();

    [RelayCommand]
    public async Task ToggleAsync()
    {
        if (IsInstalled)
            await UninstallAsync();
        else
            await InstallAsync();
    }

    private async Task UpdateIntegrityAsync()
    {
        int intactItems = 0;
        if (Instructions != null)
        {
            // Wait for all IsAppliedAsync calls to complete
            var results = await Task.WhenAll(Instructions.Select(i => i.IsAppliedAsync()));
            intactItems = results.Count(applied => applied);
        }

        int totalItems = Instructions?.Count ?? 0;

        IsInstalled = intactItems != 0;
        IsIntact = intactItems == 0 || intactItems == totalItems;

        ReboundLogger.Log($"[Mod] Updated integrity for {Name}: Installed={IsInstalled}, Intact={IsIntact}, intactItems={intactItems}, totalItems={totalItems}");
    }

    public async Task<ModIntegrity> GetIntegrityAsync()
    {
        int intactItems = 0;
        if (Instructions != null)
        {
            var results = await Task.WhenAll(Instructions.Select(i => i.IsAppliedAsync()));
            intactItems = results.Count(applied => applied);
        }

        int totalItems = Instructions?.Count ?? 0;

        return intactItems == totalItems
            ? ModIntegrity.Installed
            : intactItems == 0
                ? ModIntegrity.NotInstalled
                : ModIntegrity.Corrupt;
    }
}