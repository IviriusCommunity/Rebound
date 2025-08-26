using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rebound.Forge;
using Rebound.Helpers;

namespace Rebound.ViewModels;

public partial class ReboundViewModel : ObservableObject
{
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
    public partial bool InstallAppwiz { get; set; }

    [ObservableProperty]
    public partial bool InstallWindowsTools { get; set; }

    [ObservableProperty]
    public partial bool IsUpdateAvailable { get; set; }

    [ObservableProperty]
    public partial string VersionText { get; set; } = "";

    [ObservableProperty]
    public partial string CurrentVersion { get; set; } = "";

    bool _isLoading;

    public ReboundViewModel()
    {
        _isLoading = true;
        IsReboundEnabled = WorkingEnvironment.IsReboundEnabled();
        InstallRun = SettingsHelper.GetValue<bool>("InstallRun", "rebound", true);
        InstallShutdownDialog = SettingsHelper.GetValue<bool>("InstallShutdownDialog", "rebound", true);
        InstallShortcuts = SettingsHelper.GetValue<bool>("InstallShortcuts", "rebound", true);
        InstallThisAppCantRunOnYourPC = SettingsHelper.GetValue<bool>("InstallThisAppCantRunOnYourPC", "rebound", true);
        InstallAppwiz = SettingsHelper.GetValue<bool>("InstallAppwiz", "rebound", true);
        InstallWindowsTools = SettingsHelper.GetValue<bool>("InstallWindowsTools", "rebound", true);
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
            CurrentVersion = content;
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

    partial void OnInstallAppwizChanged(bool oldValue, bool newValue)
    {
        SettingsHelper.SetValue<bool>("InstallAppwiz", "rebound", newValue);
    }

    partial void OnInstallWindowsToolsChanged(bool oldValue, bool newValue)
    {
        SettingsHelper.SetValue<bool>("InstallWindowsTools", "rebound", newValue);
    }

    async partial void OnIsReboundEnabledChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            WorkingEnvironment.EnsureFolderIntegrity();
            WorkingEnvironment.EnsureTasksFolderIntegrity();
            WorkingEnvironment.EnsureMandatoryInstructionsIntegrity();
            if (newValue != oldValue && !_isLoading)
            {
                WorkingEnvironment.UpdateVersion();
                CheckForUpdates();
            }
        }
        else
        {
            foreach (var instruction in Catalog.MandatoryMods)
            {
                await instruction.UninstallAsync();
            }
            WorkingEnvironment.RemoveFolder();
            WorkingEnvironment.RemoveTasksFolder();
            WorkingEnvironment.RemoveMandatoryInstructions();
        }
    }

    [RelayCommand]
    public async Task UpdateOrRepairAllAsync()
    {
        foreach (var instruction in Catalog.MandatoryMods)
        {
            if (instruction.IsInstalled) await instruction.InstallAsync().ConfigureAwait(true);
        }
        foreach (var instruction in Catalog.MandatoryMods)
        {
            await instruction.InstallAsync().ConfigureAwait(true);
        }
        WorkingEnvironment.UpdateVersion();
        CheckForUpdates();
    }

    [RelayCommand]
    public void EnableRebound() => IsReboundEnabled = true;

    [RelayCommand]
    public void DisableRebound() => IsReboundEnabled = false;
}