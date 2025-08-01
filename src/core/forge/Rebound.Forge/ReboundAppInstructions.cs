using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;

namespace Rebound.Forge;

internal partial class ReboundAppInstructions : ObservableObject
{
    private bool _suppress;

    [ObservableProperty]
    public partial bool IsInstalled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsIntact { get; set; } = true;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string EntryExecutable { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string InstallationSteps { get; set; } = string.Empty;

    public InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Extras;

    public required ObservableCollection<IReboundAppInstruction> Instructions { get; set; }

    public string ProcessName { get; set; } = string.Empty;

    public ReboundAppInstructions() => Load();

    public async void Load()
    {
        _suppress = true;
        await Task.Delay(100);
        IsInstalled = GetIntegrity() == ReboundAppIntegrity.Installed;
        IsIntact = GetIntegrity() != ReboundAppIntegrity.Corrupt;
        await Task.Delay(100);
        _suppress = false;
    }

    [RelayCommand]
    public async Task RepairAsync()
    {
        await InstallAsync();
    }

    [RelayCommand]
    public async Task InstallAsync()
    {
        // Find all running processes with the target name
        var runningProcesses = Process.GetProcessesByName(ProcessName).ToList();
        bool wasRunning = runningProcesses.Count != 0;

        // Save executable paths before killing the processes
        var pathsToRestart = new List<string>();
        foreach (var process in runningProcesses)
        {
            try
            {
                var path = process.MainModule?.FileName;
                if (!string.IsNullOrEmpty(path))
                {
                    pathsToRestart.Add(path);
                }
            }
            catch
            {
                // MainModule can throw if process is protected or 64/32 bit mismatch
            }
        }

        // Kill the running processes
        foreach (var process in runningProcesses)
        {
            try
            {
                process.Kill();
            }
            catch
            {
                // Optional: handle access denied or already-exited processes
            }
        }

        await Task.Delay(200);

        // Apply install instructions
        await Task.Run(() =>
        {
            foreach (var instruction in Instructions)
            {
                instruction.Apply();
            }
        });

        // Update status
        IsInstalled = GetIntegrity() == ReboundAppIntegrity.Installed;
        IsIntact = GetIntegrity() != ReboundAppIntegrity.Corrupt;

        // Restart processes if needed
        if (wasRunning)
        {
            foreach (var path in pathsToRestart.Distinct())
            {
                try
                {
                    Process.Start(path);
                }
                catch
                {
                    // Optional: log or notify about failed restarts
                }
            }
        }
    }

    [RelayCommand]
    public async Task ToggleAsync()
    {
        if (IsInstalled)
        {
            await UninstallAsync();
        }
        else
        {
            await InstallAsync();
        }
    }

    [RelayCommand]
    public async Task UninstallAsync()
    {
        Process.GetProcessesByName(ProcessName).ToList().ForEach(p => p.Kill());

        await Task.Delay(200);

        await Task.Run(() =>
        {
            foreach (var instruction in Instructions)
            {
                instruction.Remove();
            }
        });

        IsInstalled = GetIntegrity() == ReboundAppIntegrity.Installed;
        IsIntact = GetIntegrity() != ReboundAppIntegrity.Corrupt;
    }

    public ReboundAppIntegrity GetIntegrity()
    {
        var intactItems = 0;
        var totalItems = Instructions?.Count;

        foreach (var instruction in Instructions ?? [])
        {
            if (instruction.IsApplied()) intactItems++;
        }

        return intactItems == totalItems ? ReboundAppIntegrity.Installed : intactItems == 0 ? ReboundAppIntegrity.NotInstalled : ReboundAppIntegrity.Corrupt;
    }
}