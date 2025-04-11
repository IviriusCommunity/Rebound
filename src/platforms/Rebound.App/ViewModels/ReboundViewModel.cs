using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rebound.Helpers.Modding;
using Rebound.Modding.Instructions;

namespace Rebound.ViewModels;

public partial class ReboundViewModel : ObservableObject
{
    public WinverInstructions WinverInstructions { get; set; } = new();

    public OnScreenKeyboardInstructions OnScreenKeyboardInstructions { get; set; } = new();

    public ShellInstructions ShellInstructions { get; set; } = new();

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
            await WinverInstructions.Uninstall();
            await OnScreenKeyboardInstructions.Uninstall();
            await ShellInstructions.Uninstall();
            ReboundWorkingEnvironment.RemoveFolder();
            ReboundWorkingEnvironment.RemoveTasksFolder();
        }
    }

    [RelayCommand]
    public void EnableRebound() => IsReboundEnabled = true;

    [RelayCommand]
    public void DisableRebound() => IsReboundEnabled = false;
}