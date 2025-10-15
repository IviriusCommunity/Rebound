using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rebound.Forge;

internal partial class Mod : ObservableObject
{
    [ObservableProperty] public partial bool IsInstalled { get; set; } = false;
    [ObservableProperty] public partial bool IsIntact { get; set; } = true;

    public string Name { get; }
    public string Description { get; }
    public string EntryExecutable { get; set; } = string.Empty;
    public string Icon { get; }
    public string InstallationSteps { get; }
    public InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Extras;
    public ObservableCollection<ICog> Instructions { get; }
    public ObservableCollection<ModSetting>? Settings { get; set; } = new();
    public string ProcessName { get; }

    public Mod(
        string name,
        string description,
        string icon,
        string installationSteps,
        ObservableCollection<ICog> instructions,
        string processName,
        ObservableCollection<ModSetting>? settings = null)
    {
        Name = name;
        Description = description;
        Icon = icon;
        InstallationSteps = installationSteps;
        Instructions = instructions;
        ProcessName = processName;
        Settings = settings;

        UpdateIntegrity();
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
                instruction.Apply();

            // Update status
            UpdateIntegrity();

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
                instruction.Remove();

            UpdateIntegrity();

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

    private void UpdateIntegrity()
    {
        int intactItems = Instructions?.Count(i => i.IsApplied()) ?? 0;
        int totalItems = Instructions?.Count ?? 0;

        IsInstalled = intactItems != 0;
        IsIntact = intactItems == 0 || intactItems == totalItems;

        ReboundLogger.Log($"[Mod] Updated integrity for {Name}: Installed={IsInstalled}, Intact={IsIntact}, intactItems={intactItems}, totalItems={totalItems}");
    }

    public ModIntegrity GetIntegrity()
    {
        int intactItems = Instructions?.Count(i => i.IsApplied()) ?? 0;
        int totalItems = Instructions?.Count ?? 0;

        return intactItems == totalItems
            ? ModIntegrity.Installed
            : intactItems == 0
                ? ModIntegrity.NotInstalled
                : ModIntegrity.Corrupt;
    }
}