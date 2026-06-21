// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Controls;
using Microsoft.Win32;
using Rebound.Core.Environment;
using Rebound.Core.Native.Windows;
using Rebound.Core.UI;
using Rebound.Forge;
using Rebound.Forge.Engines;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net.ServerSentEvents;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace Rebound.ControlPanel.ViewModels;

internal partial class BootAndBsodConfigurationViewModel : ObservableObject
{
    [ObservableProperty] public partial bool IsElevated { get; set; }

    [ObservableProperty] public partial int ProcessorScheduling { get; set; }

    [ObservableProperty] public partial int PagefileMode { get; set; }

    [ObservableProperty] public partial bool IsPagefileModeCustom { get; set; }

    [ObservableProperty] public partial string PagefileSize { get; set; }

    [ObservableProperty] public partial int Timeout { get; set; }

    [ObservableProperty] public partial bool NoGuiBoot { get; set; }

    [ObservableProperty] public partial bool BootLog { get; set; }

    [ObservableProperty] public partial bool BaseVideo { get; set; }

    [ObservableProperty] public partial bool OsBootInfo { get; set; }

    [ObservableProperty] public partial bool AutoRestart { get; set; }

    [ObservableProperty] public partial bool LogEvent { get; set; }

    [ObservableProperty] public partial int DumpType { get; set; }

    [ObservableProperty] public partial string DumpDirectory { get; set; }

    [ObservableProperty] public partial bool RequiresRestart { get; set; }

    private static readonly (string Name, long Factor)[] Units =
    [
        ("PB", 1024L * 1024 * 1024 * 1024 * 1024),
        ("TB", 1024L * 1024 * 1024 * 1024),
        ("GB", 1024L * 1024 * 1024),
        ("MB", 1024L * 1024),
        ("KB", 1024L),
        ("B", 1)
    ];

    [RelayCommand]
    public static void RelaunchAsAdmin()
    {
        App.SingleInstanceAppService.Relaunch(new InstanceRelaunchOptions
        {
            Elevated = true,
            ShutdownCurrent = true,
            ForceNewInstance = true,
            Arguments = CplArgs.BOOT_AND_BSOD_CONFIGURATION
        });
    }

    public BootAndBsodConfigurationViewModel()
    {
        // Properties
        IsElevated = ApplicationEnvironment.IsRunningAsAdmin();

        if (IsElevated)
        {
            // Processor scheduling
            ProcessorScheduling = RegistrySettingsEngine.GetValue<int>(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.ProcessorScheduling) switch
            {
                0x26 => 0,
                0x18 => 1,
                _ => 0
            };

            // Pagefile config
            bool auto = RegistrySettingsEngine.GetBool(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.AutomaticManagedPagefile,
                true);

            var pagingFiles = RegistrySettingsEngine.GetValue<string[]>(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.PagingFiles);

            if (auto)
            {
                PagefileMode = 0; // Auto
            }
            else
            {
                PagefileMode = (pagingFiles == null || pagingFiles.Length == 0)
                    ? 1 // No pagefile
                    : 2; // Custom
            }

            // Pagefile size
            if (PagefileMode == 2 &&
                pagingFiles?.Length > 0)
            {
                var entry = pagingFiles[0];
                var parts = entry.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length >= 3 &&
                    long.TryParse(parts[1], out var mb))
                {
                    PagefileSize = ToHumanReadable(mb * 1024 * 1024);
                }
            }

            // Boot timeout
            Timeout = RegistrySettingsEngine.GetValue<int>(RegistryHive.LocalMachine, RegistrySettingsCatalog.BootloaderTimeout);

            // Boot parameters
            NoGuiBoot = RegistrySettingsEngine.GetBool(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.NoGuiBoot);

            BootLog = RegistrySettingsEngine.GetBool(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.BootLog);

            BaseVideo = RegistrySettingsEngine.GetBool(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.BaseVideo);

            OsBootInfo = RegistrySettingsEngine.GetBool(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.OsBootInfo);

            // BSoD
            AutoRestart = RegistrySettingsEngine.GetBool(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.AutoReboot);

            LogEvent = RegistrySettingsEngine.GetBool(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.LogEvent);

            // Dump type
            DumpType = RegistrySettingsEngine.GetValue<int>(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.CrashDumpEnabled) switch
            {
                0 => 0,
                1 => 1,
                2 => 2,
                3 => 3,
                7 => 4,
                _ => 3,
            };

            // Dump directory
            DumpDirectory = RegistrySettingsEngine.GetValue<string>(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.MinidumpDir,
                "%SystemRoot%\\Minidump");
        }

        PropertyChanged += BootAndBsodConfigurationViewModel_PropertyChanged;
    }

    private void BootAndBsodConfigurationViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            // Processor scheduling
            case nameof(ProcessorScheduling):
                {
                    RegistrySettingsEngine.SetValue(
                        RegistryHive.LocalMachine,
                        RegistrySettingsCatalog.ProcessorScheduling,
                        ProcessorScheduling switch
                        {
                            0 => 0x26,
                            1 => 0x18,
                            _ => 0x26
                        });
                    break;
                }

            // Pagefile config
            case nameof(PagefileMode):
                {
                    switch (PagefileMode)
                    {
                        case 0: // Auto
                            RegistrySettingsEngine.SetBool(
                                RegistryHive.LocalMachine,
                                RegistrySettingsCatalog.AutomaticManagedPagefile,
                                true);
                            break;

                        case 1: // None
                            RegistrySettingsEngine.SetBool(
                                RegistryHive.LocalMachine,
                                RegistrySettingsCatalog.AutomaticManagedPagefile,
                                false);

                            RegistrySettingsEngine.SetValue<string[]>(
                                RegistryHive.LocalMachine,
                                RegistrySettingsCatalog.PagingFiles,
                                [],
                                RegistryValueKind.MultiString);
                            break;

                        case 2: // Custom
                            {
                                RegistrySettingsEngine.SetBool(
                                    RegistryHive.LocalMachine,
                                    RegistrySettingsCatalog.AutomaticManagedPagefile,
                                    false);

                                var pagingFiles = RegistrySettingsEngine.GetValue<string[]>(
                                    RegistryHive.LocalMachine,
                                    RegistrySettingsCatalog.PagingFiles);

                                if (pagingFiles?.Length > 0)
                                {
                                    var parts = pagingFiles[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);

                                    if (parts.Length >= 3 &&
                                        long.TryParse(parts[1], out var mb))
                                    {
                                        PagefileSize = ToHumanReadable(mb * 1024 * 1024);
                                    }
                                }

                                break;
                            }
                    }
                    IsPagefileModeCustom = PagefileMode == 2;
                    break;
                }
            // Pagefile size
            case nameof(PagefileSize):
                {
                    if (PagefileMode != 2)
                        return;

                    if (!TryParseToBytes(PagefileSize, out var bytes))
                        return;

                    var mb = BytesToMB(bytes);
                    var entry = $@"C:\pagefile.sys {mb} {mb}";

                    RegistrySettingsEngine.SetBool(
                        RegistryHive.LocalMachine,
                        RegistrySettingsCatalog.AutomaticManagedPagefile,
                        false);

                    RegistrySettingsEngine.SetValue<string[]>(
                        RegistryHive.LocalMachine,
                        RegistrySettingsCatalog.PagingFiles,
                        new[] { entry },
                        RegistryValueKind.MultiString);

                    break;
                }
            case nameof(Timeout):
                {
                    RegistrySettingsEngine.SetValue(
                        RegistryHive.LocalMachine,
                        RegistrySettingsCatalog.BootloaderTimeout,
                        Timeout);
                    break;
                }
            case nameof(NoGuiBoot):
                RegistrySettingsEngine.SetBool(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.NoGuiBoot,
                    NoGuiBoot);
                break;

            case nameof(BootLog):
                RegistrySettingsEngine.SetBool(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.BootLog,
                    BootLog);
                break;

            case nameof(BaseVideo):
                RegistrySettingsEngine.SetBool(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.BaseVideo,
                    BaseVideo);
                break;

            case nameof(OsBootInfo):
                RegistrySettingsEngine.SetBool(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.OsBootInfo,
                    OsBootInfo);
                break;

            case nameof(AutoRestart):
                RegistrySettingsEngine.SetBool(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.AutoReboot,
                    AutoRestart);
                break;

            case nameof(LogEvent):
                RegistrySettingsEngine.SetBool(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.LogEvent,
                    LogEvent);
                break;

            case nameof(DumpType):
                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.CrashDumpEnabled,
                    DumpType switch
                    {
                        0 => 0,
                        1 => 1,
                        2 => 2,
                        3 => 3,
                        4 => 7,
                        _ => 3,
                    });
                break;

            case nameof(DumpDirectory):
                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.MinidumpDir,
                    DumpDirectory,
                    RegistryValueKind.String);
                break;
        }

        RequiresRestart = true;
    }

    [RelayCommand]
    public static void Restart()
        => Shutdown.RestartNow(true);

    #region Helpers

    public static bool TryParseToBytes(string input, out long bytes)
    {
        bytes = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        input = input.Trim();

        foreach (var unit in Units)
        {
            if (!input.EndsWith(unit.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            var numberPart = input[..^unit.Name.Length].Trim();

            if (double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                bytes = (long)(value * unit.Factor);
                return true;
            }
        }

        // raw bytes fallback
        return long.TryParse(input, out bytes);
    }

    public static string ToHumanReadable(long bytes)
    {
        foreach (var unit in Units)
        {
            if (bytes >= unit.Factor)
            {
                var value = (double)bytes / unit.Factor;
                return $"{value:0.##} {unit.Name}";
            }
        }

        return $"{bytes} B";
    }

    public static long BytesToMB(long bytes) => bytes / (1024 * 1024);

    #endregion
}
