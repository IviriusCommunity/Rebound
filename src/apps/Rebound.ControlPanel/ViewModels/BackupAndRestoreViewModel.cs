// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Rebound.Core.Environment;
using Rebound.Forge.Engines;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Rebound.ControlPanel.ViewModels;

internal partial class BackupAndRestoreViewModel : ObservableObject
{
    // ── Elevation ────────────────────────────────────────────────────────────

    [ObservableProperty] public partial bool IsElevated { get; set; }

    // ── System Restore ───────────────────────────────────────────────────────

    [ObservableProperty] public partial bool IsSystemProtectionEnabled { get; set; }

    [ObservableProperty] public partial double SystemRestoreCurrentUsagePercent { get; set; }
    [ObservableProperty] public partial string SystemRestoreCurrentUsageText { get; set; } = string.Empty;

    [ObservableProperty] public partial double SystemRestoreMaxUsagePercent { get; set; } = 10;
    [ObservableProperty] public partial string SystemRestoreMaxUsageText { get; set; } = "10%";

    public ObservableCollection<RestorePointItem> RestorePoints { get; } = [];
    [ObservableProperty] public partial RestorePointItem? SelectedRestorePoint { get; set; }

    // ── Backup & Restore ─────────────────────────────────────────────────────

    [ObservableProperty] public partial bool IsWindowsBackupEnabled { get; set; }

    public ObservableCollection<BackupItem> Backups { get; } = [];
    [ObservableProperty] public partial BackupItem? SelectedBackup { get; set; }

    [ObservableProperty] public partial bool IsAutoBackupEnabled { get; set; }
    [ObservableProperty] public partial string AutoBackupScheduleText { get; set; } = string.Empty;

    // ── Registry monitor ─────────────────────────────────────────────────────

    private Thread? _registryMonitorThread;
    private CancellationTokenSource? _monitorCts;

    // ─────────────────────────────────────────────────────────────────────────

    public BackupAndRestoreViewModel()
    {
        IsElevated = ApplicationEnvironment.IsRunningAsAdmin();
        LoadSystemRestoreSettings();
        LoadBackupSettings();
        StartRegistryMonitor();
    }

    // ── Load ─────────────────────────────────────────────────────────────────

    private void LoadSystemRestoreSettings()
    {
        // System Protection enabled: HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore
        // RPSessionInterval > 0 means enabled for the drive
        var interval = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore",
            "RPSessionInterval", 0);
        IsSystemProtectionEnabled = interval > 0;

        // Disk usage: DiskPercent (max %)
        var maxPercent = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore",
            "DiskPercent", 10);
        SystemRestoreMaxUsagePercent = maxPercent;
        SystemRestoreMaxUsageText = $"{maxPercent}%";

        // Current usage: query WMI SystemRestore or stub for now
        // TODO: query actual usage via SystemRestore WMI class
        SystemRestoreCurrentUsagePercent = 0;
        SystemRestoreCurrentUsageText = "0% (0 GB of 0 GB)";

        LoadRestorePoints();
    }

    private void LoadRestorePoints()
    {
        RestorePoints.Clear();

        // TODO: enumerate via WMI SystemRestore class
        // using var searcher = new ManagementObjectSearcher("SELECT * FROM SystemRestore");
        // For now, leave empty — external tools can populate via WMI directly.
    }

    private void LoadBackupSettings()
    {
        // Windows Backup enabled: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup
        IsWindowsBackupEnabled = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup",
            "DisableMonitoring", 0) == 0;

        // Auto backup schedule
        LoadAutoBackupScheduleText();

        IsAutoBackupEnabled = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup\AutoBackup",
            "Frequency", 0) > 0;

        LoadBackups();
    }

    private void LoadAutoBackupScheduleText()
    {
        // Frequency: 1 = Daily, 2 = Weekly, 3 = Monthly
        var frequency = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup\AutoBackup",
            "Frequency", 0);

        var hour = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup\AutoBackup",
            "Hour", 19);

        var day = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup\AutoBackup",
            "Day", 0);

        var timeStr = $"{hour:D2}:00";

        AutoBackupScheduleText = frequency switch
        {
            1 => $"Daily at {timeStr}",
            2 => $"Every {(DayOfWeek)day} at {timeStr}",
            3 => $"Monthly at {timeStr}",
            _ => string.Empty
        };
    }

    private void LoadBackups()
    {
        Backups.Clear();

        // TODO: enumerate via wbadmin or WindowsImageBackup folder scan
        // Each BackupItem gets its commands wired here so DataTemplate bindings
        // work without RelativeSource or static VM references.
    }

    // ── Registry monitor ─────────────────────────────────────────────────────
    // Watches the AutoBackup key for external changes (other tools, scripts,
    // Group Policy) and refreshes AutoBackupScheduleText on the UI thread.

    private void StartRegistryMonitor()
    {
        _monitorCts = new CancellationTokenSource();
        var token = _monitorCts.Token;

        _registryMonitorThread = new Thread(() =>
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup\AutoBackup",
                    writable: false);

                if (key is null)
                    return;

                // Get the native handle via reflection — RegistryKey.Handle is public in .NET 6+
                var handle = key.Handle;

                while (!token.IsCancellationRequested)
                {
                    // Wait for any value change under this key, no subtree, 1 s timeout
                    var ret = NativeMethods.RegNotifyChangeKeyValue(
                        handle.DangerousGetHandle(),
                        bWatchSubtree: false,
                        dwNotifyFilter: NativeMethods.REG_NOTIFY_CHANGE_LAST_SET,
                        hEvent: IntPtr.Zero,
                        fAsynchronous: false);

                    if (token.IsCancellationRequested)
                        break;

                    if (ret == 0)
                    {
                        // Post back to UI thread
                        LoadAutoBackupScheduleText();
                    }
                }
            }
            catch
            {
                // Key may not exist yet — that's fine, monitor exits cleanly
            }
        })
        {
            IsBackground = true,
            Name = "AutoBackup Registry Monitor"
        };

        _registryMonitorThread.Start();
    }

    public void StopRegistryMonitor()
    {
        _monitorCts?.Cancel();
    }

    // ── Property change reactions ─────────────────────────────────────────────

    partial void OnSystemRestoreMaxUsagePercentChanged(double value)
    {
        SystemRestoreMaxUsageText = $"{value:0}%";

        if (!IsElevated)
            return;

        RegistrySettingsEngine.EnsureKeyExists(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore");

        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore",
            "DiskPercent",
            (int)value);
    }

    partial void OnIsSystemProtectionEnabledChanged(bool value)
    {
        if (!IsElevated)
            return;

        // RPSessionInterval: 0 = disabled, 86400 = enabled (once per day minimum)
        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore",
            "RPSessionInterval",
            value ? 86400 : 0);
    }

    partial void OnIsWindowsBackupEnabledChanged(bool value)
    {
        if (!IsElevated)
            return;

        RegistrySettingsEngine.EnsureKeyExists(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup");

        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup",
            "DisableMonitoring",
            value ? 0 : 1);
    }

    partial void OnIsAutoBackupEnabledChanged(bool value)
    {
        if (!IsElevated)
            return;

        // Toggling off sets Frequency to 0; toggling on restores Weekly (2) as default
        // if no frequency was previously set.
        if (!value)
        {
            RegistrySettingsEngine.SetValue(
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup\AutoBackup",
                "Frequency", 0);
        }
        else
        {
            var existing = RegistrySettingsEngine.GetValue<int>(
                RegistryHive.LocalMachine,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup\AutoBackup",
                "Frequency", 0);

            if (existing == 0)
            {
                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup\AutoBackup",
                    "Frequency", 2);
            }
        }

        LoadAutoBackupScheduleText();
    }

    // ── Commands — Elevation ─────────────────────────────────────────────────

    [RelayCommand]
    private static void RelaunchAsAdmin()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Process.GetCurrentProcess().MainModule?.FileName,
            Verb = "runas",
            UseShellExecute = true,
            //Arguments = "--newinstance " + CplArgs.BackupAndRestoreControlPanelExePath
        });
        Process.GetCurrentProcess().Kill();
    }

    // ── Commands — System Restore ────────────────────────────────────────────

    // CreateRestorePoint is initiated from the code-behind (child window),
    // which passes the description and SrSetRestorePoint event type constant.
    public async Task CreateRestorePointAsync(string description, int eventType)
    {
        if (!IsElevated || string.IsNullOrWhiteSpace(description))
            return;

        await Task.Run(() =>
        {
            // TODO: invoke System Restore COM API (SRSetRestorePoint via srrestoreptapi.dll)
            // RESTOREPOINTINFO rp;
            // rp.dwEventType = eventType;
            // rp.dwRestorePtType = MODIFY_SETTINGS;
            // rp.llSequenceNumber = 0;
            // rp.szDescription = description;
            // SRSetRestorePoint(&rp, &smgr);
        });

        LoadRestorePoints();
    }

    [RelayCommand]
    private async Task RestoreSelectedRestorePointAsync()
    {
        if (!IsElevated || SelectedRestorePoint is null)
            return;

        await Task.Run(() =>
        {
            // TODO: invoke rstrui.exe /runonce or System Restore COM with sequence number
        });
    }

    [RelayCommand]
    private async Task DeleteAllRestorePointsAsync()
    {
        if (!IsElevated)
            return;

        await Task.Run(() =>
        {
            // TODO: SRRemoveRestorePoint for all sequence numbers, or
            // vssadmin delete shadows /for=C: /quiet
        });

        LoadRestorePoints();
    }

    // ── Commands — Backup ────────────────────────────────────────────────────

    // ConfigureAutoBackup is initiated from the code-behind (child window).
    // The child window writes directly to the registry; we just reload here.
    public void OnAutoBackupConfigured()
    {
        LoadAutoBackupScheduleText();

        IsAutoBackupEnabled = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsBackup\AutoBackup",
            "Frequency", 0) > 0;
    }

    [RelayCommand]
    private async Task StartBackupAsync()
    {
        if (!IsElevated)
            return;

        await Task.Run(() =>
        {
            // TODO: wbadmin start backup with configured targets
        });

        LoadBackups();
    }

    [RelayCommand]
    private async Task StartSystemImageBackupAsync()
    {
        if (!IsElevated)
            return;

        await Task.Run(() =>
        {
            // TODO: wbadmin start sysrecovery or wbadmin start backup -allcritical
        });

        LoadBackups();
    }

    [RelayCommand]
    public async Task RestoreForMeAsync(BackupItem? backup)
    {
        if (!IsElevated || backup is null)
            return;

        await Task.Run(() =>
        {
            // TODO: wbadmin start recovery -version:<backup.Version> -itemtype:File
            //       -items:<user profile path> -recursive -overwrite:yes
        });
    }

    [RelayCommand]
    public async Task RestoreForAllUsersAsync(BackupItem? backup)
    {
        if (!IsElevated || backup is null)
            return;

        await Task.Run(() =>
        {
            // TODO: wbadmin start recovery for all user profiles
        });
    }

    // RestoreFromFile is initiated from the code-behind (file picker).
    public async Task RestoreFromFileAsync(string path)
    {
        if (!IsElevated || string.IsNullOrWhiteSpace(path))
            return;

        await Task.Run(() =>
        {
            // TODO: mount VHD / call wbadmin with explicit path
        });
    }

    [RelayCommand]
    public void OpenBackupInExplorer(BackupItem? backup)
    {
        if (backup is null || string.IsNullOrWhiteSpace(backup.Location))
            return;

        var dir = Path.GetDirectoryName(backup.Location);
        if (string.IsNullOrWhiteSpace(dir))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{dir}\"",
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }

    [RelayCommand]
    public async Task DeleteBackupAsync(BackupItem? backup)
    {
        if (!IsElevated || backup is null)
            return;

        await Task.Run(() =>
        {
            // TODO: wbadmin delete backup -version:<backup.Version> -quiet
        });

        Backups.Remove(backup);
    }

    [RelayCommand]
    private static void LaunchRecoveryDriveWizard()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.System),
                    "recoverydrive.exe"),
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }
}

// ── Model classes ─────────────────────────────────────────────────────────────

internal sealed partial class RestorePointItem : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; } = string.Empty;
    [ObservableProperty] public partial string Description { get; set; } = string.Empty;
    [ObservableProperty] public partial DateTime CreatedOn { get; set; }
    [ObservableProperty] public partial string SizeText { get; set; } = string.Empty;

    public string CreatedOnText => $"Created on: {CreatedOn:dd.MM.yyyy}";
}

internal sealed partial class BackupItem : ObservableObject
{
    [ObservableProperty] public partial DateTime CreatedOn { get; set; }
    [ObservableProperty] public partial string SizeText { get; set; } = string.Empty;
    [ObservableProperty] public partial string Location { get; set; } = string.Empty;

    [ObservableProperty] public partial bool IncludesLibraries { get; set; }
    [ObservableProperty] public partial bool IncludesSelectedFolders { get; set; }
    [ObservableProperty] public partial bool IncludesSystemImage { get; set; }

    // Wired at load time so DataTemplate x:Bind can reach VM commands
    // without RelativeSource or static references.
    public Action<BackupItem>? OpenInExplorerAction { get; set; }
    public Action<BackupItem>? DeleteAction { get; set; }
    public Func<BackupItem, Task>? RestoreForMeAction { get; set; }
    public Func<BackupItem, Task>? RestoreForAllUsersAction { get; set; }

    public string CreatedOnText => $"Created on: {CreatedOn:dd.MM.yyyy}";

    [RelayCommand]
    private void OpenInExplorer() => OpenInExplorerAction?.Invoke(this);

    [RelayCommand]
    private void Delete() => DeleteAction?.Invoke(this);

    [RelayCommand]
    private Task RestoreForMe() => RestoreForMeAction?.Invoke(this) ?? Task.CompletedTask;

    [RelayCommand]
    private Task RestoreForAllUsers() => RestoreForAllUsersAction?.Invoke(this) ?? Task.CompletedTask;
}

// ── Native interop ────────────────────────────────────────────────────────────

internal static class NativeMethods
{
    internal const int REG_NOTIFY_CHANGE_LAST_SET = 0x00000004;

    [System.Runtime.InteropServices.DllImport("advapi32.dll")]
    internal static extern int RegNotifyChangeKeyValue(
        IntPtr hKey,
        bool bWatchSubtree,
        int dwNotifyFilter,
        IntPtr hEvent,
        bool fAsynchronous);
}