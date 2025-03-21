using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Rebound.Helpers.Modding;

public abstract partial class ReboundAppInstructions : ObservableObject
{
    [ObservableProperty]
    public partial bool IsInstalled { get; set; } = false;

    async partial void OnIsInstalledChanged(bool oldValue, bool newValue)
    {
        if (newValue) await Install();
        else await Uninstall();
    }

    public virtual InstallationTemplate PreferredInstallationTemplate { get; set; } = InstallationTemplate.Extras;

    public virtual ObservableCollection<IReboundAppInstruction>? Instructions { get; set; }

    public ReboundAppInstructions()
    {
        IsInstalled = GetIntegrity() == ReboundAppIntegrity.Installed;
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