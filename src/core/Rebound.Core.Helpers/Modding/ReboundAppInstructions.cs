﻿using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Rebound.Helpers.Modding;

public abstract partial class ReboundAppInstructions : ObservableObject
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

    public virtual InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Extras;

    public virtual ObservableCollection<IReboundAppInstruction>? Instructions { get; set; }

    protected ReboundAppInstructions()
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
        await Task.Run(() =>
        {
            foreach (var instruction in Instructions)
            {
                instruction.Apply();
            }
        });

        IsInstalled = GetIntegrity() == ReboundAppIntegrity.Installed;
        IsIntact = GetIntegrity() != ReboundAppIntegrity.Corrupt;
    }

    public async Task Uninstall()
    {
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