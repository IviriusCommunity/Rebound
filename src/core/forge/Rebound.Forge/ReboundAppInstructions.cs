using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Rebound.Forge;

public partial class ReboundAppInstructions : ObservableObject
{
    [ObservableProperty]
    public partial bool IsInstalled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsIntact { get; set; } = true;

    async partial void OnIsInstalledChanged(bool oldValue, bool newValue)
    {
        if (newValue) await Install();
        else await Uninstall();
    }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string EntryExecutable { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string InstallationSteps { get; set; } = string.Empty;

    public InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Extras;

    public ObservableCollection<IReboundAppInstruction>? Instructions { get; set; }

    public string ProcessName { get; set; }

    public ReboundAppInstructions()
    {
        IsInstalled = GetIntegrity() == ReboundAppIntegrity.Installed;
        IsIntact = GetIntegrity() != ReboundAppIntegrity.Corrupt;
    }

    [RelayCommand]
    public async Task Repair()
    {
        await Install();
    }

    public async Task Install()
    {
        // Check if any process with ProcessName is running
        var runningProcesses = Process.GetProcessesByName(ProcessName).ToList();
        bool wasRunning = runningProcesses.Any();

        // Kill running processes
        runningProcesses.ForEach(p => p.Kill());
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
            try
            {
                foreach (var p in runningProcesses)
                {
                    Process.Start(p.MainModule?.FileName ?? ProcessName);
                }
            }
            catch
            {
                // Optional: handle any errors starting the processes
            }
        }
    }

    public async Task Uninstall()
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

        foreach (var instruction in Instructions)
        {
            if (instruction.IsApplied()) intactItems++;
        }

        return intactItems == totalItems ? ReboundAppIntegrity.Installed : intactItems == 0 ? ReboundAppIntegrity.NotInstalled : ReboundAppIntegrity.Corrupt;
    }
}