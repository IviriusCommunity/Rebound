using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rebound.Forge;
using Rebound.Modding;
using Rebound.Modding.Instructions;

namespace Rebound.ViewModels;

public partial class ReboundViewModel : ObservableObject
{
    public ObservableCollection<UserInterfaceReboundAppInstructions> Instructions { get; } =
    [
        new WinverInstructions(),
        new OnScreenKeyboardInstructions(),
        new DiskCleanupInstructions(),
        new UserAccountControlSettingsInstructions(),
        new ControlPanelInstructions(),
        /*new ShellInstructions()*/ // To be reimplemented Soon™
    ];

    [ObservableProperty]
    public partial bool IsReboundEnabled { get; set; }

    public ReboundViewModel()
    {
        IsReboundEnabled = ReboundWorkingEnvironment.IsReboundEnabled();
    }

    async partial void OnIsReboundEnabledChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            ReboundWorkingEnvironment.EnsureFolderIntegrity();
            ReboundWorkingEnvironment.EnsureTasksFolderIntegrity();
        }
        else
        {
            foreach (var instruction in Instructions)
            {
                await instruction.Uninstall();
            }
            ReboundWorkingEnvironment.RemoveFolder();
            ReboundWorkingEnvironment.RemoveTasksFolder();
        }
    }

    [RelayCommand]
    public async Task UpdateOrRepairAllAsync()
    {
        foreach (var instruction in Instructions)
        {
            if (instruction.IsInstalled) await instruction.Install().ConfigureAwait(true);
        }
    }

    [RelayCommand]
    public void EnableRebound() => IsReboundEnabled = true;

    [RelayCommand]
    public void DisableRebound() => IsReboundEnabled = false;
}