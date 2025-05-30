﻿using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rebound.Forge;
using Rebound.Helpers;
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
        new ShellInstructions()
    ];

    [ObservableProperty]
    public partial bool IsReboundEnabled { get; set; }

    [ObservableProperty]
    public partial bool InstallRun { get; set; }

    [ObservableProperty]
    public partial bool InstallShutdownDialog { get; set; }

    [ObservableProperty]
    public partial bool InstallShortcuts { get; set; }

    [ObservableProperty]
    public partial bool InstallThisAppCantRunOnYourPC { get; set; }

    [ObservableProperty]
    public partial bool IsUpdateAvailable { get; set; }

    [ObservableProperty]
    public partial string VersionText { get; set; }

    bool _isLoading;

    public ReboundViewModel()
    {
        _isLoading = true;
        IsReboundEnabled = ReboundWorkingEnvironment.IsReboundEnabled();
        InstallRun = SettingsHelper.GetValue<bool>("InstallRun", "rebound", true);
        InstallShutdownDialog = SettingsHelper.GetValue<bool>("InstallShutdownDialog", "rebound", true);
        InstallShortcuts = SettingsHelper.GetValue<bool>("InstallShortcuts", "rebound", true);
        InstallThisAppCantRunOnYourPC = SettingsHelper.GetValue<bool>("InstallThisAppCantRunOnYourPC", "rebound", true);
        CheckForUpdates();
        _isLoading = false;
    }

    public void CheckForUpdates()
    {
        try
        {
            var programFilesPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
            var directoryPath = Path.Combine(programFilesPath, "Rebound");

            var content = File.ReadAllText(Path.Combine(directoryPath, "version.txt"));
            VersionText = $"Current version: {content}  -  New version: {Helpers.Environment.ReboundVersion.REBOUND_VERSION}";
            IsUpdateAvailable = Helpers.Environment.ReboundVersion.REBOUND_VERSION != content;
        }
        catch
        {
            IsUpdateAvailable = false;
        }
    }

    partial void OnInstallRunChanged(bool oldValue, bool newValue)
    {
        SettingsHelper.SetValue<bool>("InstallRun", "rebound", newValue);
    }

    partial void OnInstallShutdownDialogChanged(bool oldValue, bool newValue)
    {
        SettingsHelper.SetValue<bool>("InstallShutdownDialog", "rebound", newValue);
    }

    partial void OnInstallShortcutsChanged(bool oldValue, bool newValue)
    {
        SettingsHelper.SetValue<bool>("InstallShortcuts", "rebound", newValue);
    }

    partial void OnInstallThisAppCantRunOnYourPCChanged(bool oldValue, bool newValue)
    {
        SettingsHelper.SetValue<bool>("InstallThisAppCantRunOnYourPC", "rebound", newValue);
    }

    async partial void OnIsReboundEnabledChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            ReboundWorkingEnvironment.EnsureFolderIntegrity();
            ReboundWorkingEnvironment.EnsureTasksFolderIntegrity();
            if (newValue != oldValue && !_isLoading)
            {
                ReboundWorkingEnvironment.UpdateVersion();
                CheckForUpdates();
            }
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
        ReboundWorkingEnvironment.UpdateVersion();
        CheckForUpdates();
    }

    [RelayCommand]
    public void EnableRebound() => IsReboundEnabled = true;

    [RelayCommand]
    public void DisableRebound() => IsReboundEnabled = false;
}