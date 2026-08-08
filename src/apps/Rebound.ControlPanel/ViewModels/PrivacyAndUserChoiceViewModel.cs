// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Rebound.ControlPanel.Services;
using Rebound.Core.Environment;
using Rebound.Core.Native.Wrappers;
using Rebound.Core.SystemInformation.Software;
using Rebound.Core.TaskScheduler.Native;
using Rebound.Forge;
using Rebound.Forge.Cogs;
using Rebound.Forge.Engines;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using Windows.System;
using static TerraFX.Interop.Windows.HKEY;
using static TerraFX.Interop.Windows.KEY;
using static TerraFX.Interop.Windows.REG;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.ControlPanel.ViewModels;

internal partial class PrivacyAndUserChoiceViewModel : ObservableObject
{
    public PrivacyAndUserChoiceViewModel()
    {
        RefreshDMAProperties();
        RefreshUcpdProperties();
        RefreshWindowsUpdateProperties();
        RefreshCopilotProperties();
        RefreshSmartScreenProperties();
        RefreshSudoProperties();
        RefreshExecutionPolicies();
        RefreshDeveloperModeProperties();
        RefreshUacProperties();
        RefreshPrivacyHighProperties();
        RefreshPrivacyMediumProperties();
        RefreshPrivacyLowProperties();
        RefreshContentProperties();
        RefreshGetStartedProperties();
        FeedbackHub.UpdateIntegrityAsync();
        GetHelp.UpdateIntegrityAsync();
        MicrosoftStore.UpdateIntegrityAsync();
        Notepad.UpdateIntegrityAsync();
        Paint.UpdateIntegrityAsync();
        People.UpdateIntegrityAsync();
        PhoneLink.UpdateIntegrityAsync();
        SnippingTool.UpdateIntegrityAsync();
        Terminal.UpdateIntegrityAsync();
        WindowsWebExperiencePack.UpdateIntegrityAsync();
        XboxGameBar.UpdateIntegrityAsync();
        Bing.UpdateIntegrityAsync();
        Calculator.UpdateIntegrityAsync();
        Camera.UpdateIntegrityAsync();
        Clipchamp.UpdateIntegrityAsync();
        Clock.UpdateIntegrityAsync();
        Copilot.UpdateIntegrityAsync();
        MediaPlayer.UpdateIntegrityAsync();
        Microsoft365Copilot.UpdateIntegrityAsync();
        MicrosoftSolitaireCollection.UpdateIntegrityAsync();
        News.UpdateIntegrityAsync();
        Photos.UpdateIntegrityAsync();
        SoundRecorder.UpdateIntegrityAsync();
        ToDo.UpdateIntegrityAsync();
        Weather.UpdateIntegrityAsync();
        Xbox.UpdateIntegrityAsync();
        IsOneDriveInstalled = CheckIsOneDriveInstalled();
    }

    #region OneDrive

    [ObservableProperty] public partial bool IsOneDriveInstalled { get; set; }

    /// <summary>
    /// For when the regret kicks in. 
    /// Hands the URL off to the Windows shell to open in the user's default browser.
    /// </summary>
    [RelayCommand]
    public static void OpenOneDriveDownloadPage()
    {
        Launcher.LaunchUriAsync(new Uri("https://www.microsoft.com/en-us/microsoft-365/onedrive/download"));
    }

    /// <summary>
    /// Kills OneDrive, runs the official hidden uninstaller, and wipes remaining folders.
    /// </summary>
    [RelayCommand]
    public async Task UninstallOneDriveSafelyAsync()
    {
        // 1. Terminate running OneDrive processes
        foreach (var process in Process.GetProcessesByName("OneDrive"))
        {
            try { process.Kill(); } catch { /* Process might already be closing */ }
        }

        // Give it a second to release file locks
        await Task.Delay(1000);

        // 2. Locate the built-in OneDrive setup executable
        string sysWow64 = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
        string uninstallerPath = Path.Combine(sysWow64, "OneDriveSetup.exe");

        if (!File.Exists(uninstallerPath))
        {
            // Fallback for 32-bit Windows installs
            string system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            uninstallerPath = Path.Combine(system32, "OneDriveSetup.exe");
        }

        // 3. Execute the silent uninstaller
        if (File.Exists(uninstallerPath))
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = uninstallerPath,
                Arguments = "/uninstall",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
            }
        }

        // 4. Nuke the leftover directories
        string[] directoriesToClean =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "OneDrive"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft OneDrive"),
            Path.Combine(WindowsInformation.GetWindowsInstallationDrivePath(), "OneDriveTemp"),
        };

        foreach (var dir in directoriesToClean)
        {
            if (Directory.Exists(dir))
            {
                try
                {
                    Directory.Delete(dir, recursive: true);
                }
                catch
                {
                    // Silently ignore access denied errors for locked system files
                }
            }
        }
        IsOneDriveInstalled = CheckIsOneDriveInstalled();
    }

    /// <summary>
    /// Checks if OneDrive is installed by looking for the executable 
    /// in both per-user and per-machine installation directories.
    /// </summary>
    /// <returns>True if OneDrive is found, otherwise false.</returns>
    public bool CheckIsOneDriveInstalled()
    {
        // 1. Check per-user installation (Most common on Windows 11)
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string userPath = Path.Combine(localAppData, "Microsoft", "OneDrive", "OneDrive.exe");

        if (File.Exists(userPath))
        {
            return true;
        }

        // 2. Check per-machine installation (64-bit Program Files)
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string machinePath = Path.Combine(programFiles, "Microsoft OneDrive", "OneDrive.exe");

        if (File.Exists(machinePath))
        {
            return true;
        }

        // 3. Check per-machine installation (32-bit Program Files fallback)
        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string machinePathX86 = Path.Combine(programFilesX86, "Microsoft OneDrive", "OneDrive.exe");

        if (File.Exists(machinePathX86))
        {
            return true;
        }

        // Not found anywhere
        return false;
    }

    #endregion

    #region App packages

    public Mod Xbox = new()
    {
        Name = "Xbox",
        Id = new Guid("71C4A9E6-2F53-48BD-9061-D7E835A24CF9"),
        Variants =
        [
            new ModVariant
            {
                Name = "Xbox",
                Id = new Guid("C8521D74-9E36-4AB0-BF17-63A8D5E294FC"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Xbox",
                        CogId = new Guid("F36B7C15-A924-4D68-8E51-C2709AF43DB6"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9MV0B5HZVK9Z",
                            PackageFamilyName: "Microsoft.GamingApp_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod Weather = new()
    {
        Name = "Weather",
        Id = new Guid("2A7F5C91-E384-4B62-9D17-6F0A82C5E4B3"),
        Variants =
        [
            new ModVariant
            {
                Name = "Weather",
                Id = new Guid("B63D8E24-7A51-4C90-AF36-19E5D2748BC1"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Weather",
                        CogId = new Guid("E914C672-35A8-4F21-BD69-7C53A0E846D2"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9WZDNCRFJ3Q2",
                            PackageFamilyName: "Microsoft.BingWeather_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod ToDo = new()
    {
        Name = "To Do",
        Id = new Guid("F6A28D91-4C73-45E8-B2A6-917D53C04F8B"),
        Variants =
        [
            new ModVariant
            {
                Name = "To Do",
                Id = new Guid("A31E6C57-9B42-4D85-8F13-C67294E5B0AD"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "To Do",
                        CogId = new Guid("D84B215E-7639-4FA2-A8C1-5E97B3D6402F"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9NBLGGH5R558",
                            PackageFamilyName: "Microsoft.Todos_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod SoundRecorder = new()
    {
        Name = "Sound Recorder",
        Id = new Guid("3F8A6C21-94D7-4E52-B083-71C5A9F642DE"),
        Variants =
        [
            new ModVariant
            {
                Name = "Sound Recorder",
                Id = new Guid("A27E51D9-6B43-48F0-9C75-2D8A6314BEF9"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Sound Recorder",
                        CogId = new Guid("E64B92F7-15C8-4A36-A0D9-83F7C2516E4B"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9WZDNCRFHWKN",
                            PackageFamilyName: "Microsoft.WindowsSoundRecorder_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod Photos = new()
    {
        Name = "Photos",
        Id = new Guid("6F3B2A91-8D47-4C65-AE13-7590D2B846FC"),
        Variants =
        [
            new ModVariant
            {
                Name = "Photos",
                Id = new Guid("B8246D57-31FA-4E92-9C08-A7E5136BD2C4"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Photos",
                        CogId = new Guid("D19C73A4-56E8-4B21-AB95-638F0E274C1D"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9WZDNCRFJBH4",
                            PackageFamilyName: "Microsoft.Windows.Photos_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod News = new()
    {
        Name = "News",
        Id = new Guid("B7E4C91A-5D62-4F38-A807-2C96E13B75D4"),
        Variants =
        [
            new ModVariant
            {
                Name = "News",
                Id = new Guid("D35A8F62-1C74-4B09-9E53-A6D287F41CB8"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "News",
                        CogId = new Guid("F81C3E57-6A29-45D0-B914-7E52C8A63B40"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9WZDNCRFHVFW",
                            PackageFamilyName: "Microsoft.BingNews_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod MicrosoftSolitaireCollection = new()
    {
        Name = "Microsoft Solitaire Collection",
        Id = new Guid("4D8A2F71-C593-46E8-B0A4-917C65D3F829"),
        Variants =
        [
            new ModVariant
            {
                Name = "Microsoft Solitaire Collection",
                Id = new Guid("A7E34C19-6285-4F0B-95D2-C8617B4E3A50"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Microsoft Solitaire Collection",
                        CogId = new Guid("E52B96D4-7138-4AC0-BF57-29C8E641A3D7"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9WZDNCRFHWD2",
                            PackageFamilyName: "Microsoft.MicrosoftSolitaireCollection_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod Microsoft365Copilot = new()
    {
        Name = "Microsoft 365 Copilot (Office web app)",
        Id = new Guid("71D4C8A2-5F39-46BE-9A17-C063E84B25D1"),
        Variants =
        [
            new ModVariant
            {
                Name = "Microsoft 365 Copilot (Office web app)",
                Id = new Guid("E6A13F79-2C54-4D08-BB62-95F7A3C841DE"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Microsoft 365 Copilot (Office web app)",
                        CogId = new Guid("4B9E27D6-83F1-45AC-A750-C12D68E39F54"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9WZDNCRD29V9",
                            PackageFamilyName: "Microsoft.MicrosoftOfficeHub_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod MediaPlayer = new()
    {
        Name = "Media Player",
        Id = new Guid("B6F21A93-5D47-4C80-AE16-739C28F54D02"),
        Variants =
        [
            new ModVariant
            {
                Name = "Media Player",
                Id = new Guid("E84C7B25-91D3-46FA-B802-5A67CE1394F8"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Media Player",
                        CogId = new Guid("3A9E56D1-C742-4F08-BB35-8162D7E94AC6"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9WZDNCRFJ3PT",
                            PackageFamilyName: "Microsoft.ZuneMusic_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod Copilot = new()
    {
        Name = "Copilot",
        Id = new Guid("6B3E9F41-8D27-4A65-B0C4-51F72E9D83A6"),
        Variants =
        [
            new ModVariant
            {
                Name = "Copilot",
                Id = new Guid("E14A72C9-5F63-4B08-9D31-76C8E5A204BF"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Copilot",
                        CogId = new Guid("A83D51F7-2C94-46E0-B718-5E9C3A62D4F1"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9NHT9RB2F4HD",
                            PackageFamilyName: "Microsoft.Copilot_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod Clock = new()
    {
        Name = "Clock",
        Id = new Guid("5F8A1C72-3D64-4E91-B8F2-6A0C9D47E153"),
        Variants =
        [
            new ModVariant
            {
                Name = "Clock",
                Id = new Guid("A42E7B19-8C35-46D0-9F61-2B7E5A83C4D6"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Clock",
                        CogId = new Guid("D91C4E58-72A6-4F03-BD89-1C5E7A26F4B0"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9WZDNCRFJ3PR",
                            PackageFamilyName: "Microsoft.WindowsAlarms_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod Clipchamp = new()
    {
        Name = "Clipchamp",
        Id = new Guid("F2C84A61-7D35-4E92-B806-1A5F39C7E4D2"),
        Variants =
        [
            new ModVariant
            {
                Name = "Clipchamp",
                Id = new Guid("9B17E5C3-42A8-4F76-AC91-D63E28B507EF"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Clipchamp",
                        CogId = new Guid("C63A9F28-15D4-47B0-8E72-F5A1C904B6D3"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9P1J8S7CCWWT",
                            PackageFamilyName: "Clipchamp.Clipchamp_yxz26nhyzhsrt")
                    }
                ]
            }
        ]
    };

    public Mod Camera = new()
    {
        Name = "Camera",
        Id = new Guid("7D42B8E1-5C96-4A37-9F20-63E8D154AB72"),
        Variants =
        [
            new ModVariant
            {
                Name = "Camera",
                Id = new Guid("C5A91743-E268-4B90-A6D1-82F35CE7490B"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Camera",
                        CogId = new Guid("E1846F92-7A53-4C2D-B905-D7A61E38F4CB"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9WZDNCRFJBBG",
                            PackageFamilyName: "Microsoft.WindowsCamera_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod Calculator = new()
    {
        Name = "Calculator",
        Id = new Guid("8F3C1D72-A649-4B5E-92D8-C607E4A13F85"),
        Variants =
        [
            new ModVariant
            {
                Name = "Calculator",
                Id = new Guid("D41A7E96-2C53-48B9-AF17-6E82C5D30471"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Calculator",
                        CogId = new Guid("5B9E2A43-7D16-4FC8-B851-C394E6072A9D"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9WZDNCRFHVN5",
                            PackageFamilyName: "Microsoft.WindowsCalculator_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod Bing = new()
    {
        Name = "Bing",
        Id = new Guid("5A8D31E7-42C6-4F95-B1D8-73E9A26C540B"),
        Variants =
        [
            new ModVariant
            {
                Name = "Bing",
                Id = new Guid("C61F8A24-9D53-47B0-AE72-35D4C9186F0A"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Bing",
                        CogId = new Guid("E93B57C1-6A48-4D82-9F15-C27E4A6B830D"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9NZBF4GT040C",
                            PackageFamilyName: "Microsoft.BingSearch_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod XboxGameBar = new()
    {
        Name = "Xbox Game Bar",
        Id = new Guid("B7E42C91-6D35-4A8F-93C7-1E5F20D84AB6"),
        Variants =
        [
            new ModVariant
            {
                Name = "Xbox Game Bar",
                Id = new Guid("4C8A17E3-92F6-45B1-A7D9-63E0C52F8B14"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Xbox Game Bar",
                        CogId = new Guid("E6D351A9-7C24-48F0-B583-2A9E74C16D5B"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9NZKPSTSNW4P",
                            PackageFamilyName: "Microsoft.XboxGamingOverlay_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod WindowsWebExperiencePack = new()
    {
        Name = "Windows Web Experience Pack",
        Id = new Guid("D3A7F6C1-5E92-4B84-AF31-9C7D2E6085B4"),
        Variants =
        [
            new ModVariant
            {
                Name = "Windows Web Experience Pack",
                Id = new Guid("72E4B9A6-C813-4F57-8D20-B6A31E95C742"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Windows Web Experience Pack",
                        CogId = new Guid("A5C28E71-3D96-46B0-91F4-E7A52C638D09"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9MSSGKG348SP",
                            PackageFamilyName: "MicrosoftWindows.Client.WebExperience_cw5n1h2txyewy")
                    }
                ]
            }
        ]
    };

    public Mod Terminal = new()
    {
        Name = "Terminal",
        Id = new Guid("6F1E9C43-2A7D-4B85-9D16-C0E54F73A8B2"),
        Variants =
        [
            new ModVariant
            {
                Name = "Terminal",
                Id = new Guid("B8C2D741-5E39-46AF-91D2-73C8E4A6F05B"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Terminal",
                        CogId = new Guid("E4A97C12-8F56-4D3B-B0E7-2C61A9F5843D"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9N0DX20HK701",
                            PackageFamilyName: "Microsoft.WindowsTerminal_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod SnippingTool = new()
    {
        Name = "Snipping Tool",
        Id = new Guid("5C8E2A71-4D93-46B0-AF15-7E36C9D842B5"),
        Variants =
        [
            new ModVariant
            {
                Name = "Snipping Tool",
                Id = new Guid("A63F1C98-7254-4E0B-BD37-91C5E8A462F0"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Snipping Tool",
                        CogId = new Guid("D17B5E43-8C26-49F1-A072-63E94C815BD7"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9MZ95KL8MR0L",
                            PackageFamilyName: "Microsoft.ScreenSketch_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod PhoneLink = new()
    {
        Name = "Phone Link",
        Id = new Guid("3A7E91C4-5D28-46B0-8F13-C692E74A5B36"),
        Variants =
        [
            new ModVariant
            {
                Name = "Phone Link",
                Id = new Guid("E14C6B82-9375-4A0D-BF61-28D5E9037C4A"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Phone Link",
                        CogId = new Guid("72F5A1D9-4C63-48BE-A807-E9316D2F5B4C"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9NMPJ99VJBWV",
                            PackageFamilyName: "Microsoft.YourPhone_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod People = new()
    {
        Name = "People",
        Id = new Guid("7A4D2E91-6F83-4B5C-A017-C9E6D34872FA"),
        Variants =
        [
            new ModVariant
            {
                Name = "People",
                Id = new Guid("B6C1F509-38D7-4E2A-9A64-F175C8230E4B"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "People",
                        CogId = new Guid("E29A7C46-51B8-4D03-BF95-63C2E8174A0D"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9NBLGGH10PG8",
                            PackageFamilyName: "Microsoft.People_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod Paint = new()
    {
        Name = "Paint",
        Id = new Guid("6D8F2A41-C573-4B96-AE10-7C4D91F83562"),
        Variants =
        [
            new ModVariant
            {
                Name = "Paint",
                Id = new Guid("A14E6C83-29B7-45D0-9F52-E8C31A76B904"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Paint",
                        CogId = new Guid("F53B918D-64E2-4A07-B6C9-21D8753F0AE4"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9PCFS5B6T72H",
                            PackageFamilyName: "Microsoft.Paint_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod Notepad = new()
    {
        Name = "Notepad",
        Id = new Guid("4F7A1C92-6B35-4E08-AFD3-91C2B7D5E640"),
        Variants =
        [
            new ModVariant
            {
                Name = "Notepad",
                Id = new Guid("B83E5D14-29F6-47C1-9A70-E6D4B2F8135C"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Notepad",
                        CogId = new Guid("D26A8F73-51C4-4B09-BE62-7C935E1A4F80"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9MSMLRH6LZF3",
                            PackageFamilyName: "Microsoft.WindowsNotepad_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod MicrosoftStore = new()
    {
        Name = "Microsoft Store",
        Id = new Guid("5D2A8F41-C693-47BE-A105-72F9D638E2C4"),
        Variants =
        [
            new ModVariant
            {
                Name = "Microsoft Store",
                Id = new Guid("E741C529-8B36-4DA0-9F52-61A7D3C845BE"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Microsoft Store",
                        CogId = new Guid("B936E174-5C82-4AF9-AE31-7D628F450CB9"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9WZDNCRFJBMP",
                            PackageFamilyName: "Microsoft.WindowsStore_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod GetHelp = new()
    {
        Name = "Get Help",
        Id = new Guid("3F7A2C91-6E54-4D83-B9A7-1C2E8F5064D3"),
        Variants =
        [
            new ModVariant
            {
                Name = "Get Help",
                Id = new Guid("A85D4E27-91C6-47B2-8F3A-D60E5C7194AB"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Get Help",
                        CogId = new Guid("D2947B63-5A18-4CE9-AF72-83E1C6059B4D"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9PKDZBMV1H3T",
                            PackageFamilyName: "Microsoft.GetHelp_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    public Mod FeedbackHub = new()
    {
        Name = "Feedback Hub",
        Id = new Guid("9EFB1576-6460-4081-8DFC-96303E8FBD49"),
        Variants =
        [
            new ModVariant
            {
                Name = "Feedback Hub",
                Id = new Guid("C7D55740-1162-47D3-89C6-E9BC21DE8843"),
                Cogs =
                [
                    new PackageCog
                    {
                        CogName = "Feedback Hub",
                        CogId = new Guid("C84B5E29-87E4-4839-AE5E-1D7A692D3D9A"),
                        DoPackageManagementOn = PackageManagementTriggeredOn.Both,
                        Target = new PackageTarget(
                            PackageTargetType.Store,
                            StoreProductId: "9NBLGGH4R32N",
                            PackageFamilyName: "Microsoft.WindowsFeedbackHub_8wekyb3d8bbwe")
                    }
                ]
            }
        ]
    };

    #endregion

    #region Get Started

    [ObservableProperty] public partial bool IsGetStartedEnabled { get; set; }

    partial void OnIsGetStartedEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableGetStarted,
            !value);
    }

    private void RefreshGetStartedProperties()
    {
        IsGetStartedEnabled = !RegistrySettingsEngine.GetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableGetStarted,
            false);
    }

    #endregion

    #region Content

    [ObservableProperty]
    public partial bool IsSuggestedSettingsContentEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsSuggestedNotificationsEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsLockScreenTipsEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsWindowsTipsEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsWindowsWelcomeExperienceEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsFinishSettingUpDeviceEnabled { get; set; }

    partial void OnIsSuggestedSettingsContentEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.SuggestedContentSettings,
            value);

        RegistrySettingsEngine.SetBool(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.SuggestedContentSettings2,
            value);

        RegistrySettingsEngine.SetBool(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.SuggestedContentSettings3,
            value);
    }

    partial void OnIsSuggestedNotificationsEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.MicrosoftPromotionalNotifications,
            value);
    }

    partial void OnIsLockScreenTipsEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.LockScreenTipsEnabled,
            value);
    }

    partial void OnIsWindowsTipsEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.WindowsTipsEnabled,
            value);
    }

    partial void OnIsWindowsWelcomeExperienceEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.WelcomeExperienceEnabled,
            value);
    }

    partial void OnIsFinishSettingUpDeviceEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.ScoobeSystemSettingEnabled,
            value);
    }

    private void RefreshContentProperties()
    {
        IsSuggestedSettingsContentEnabled =
            RegistrySettingsEngine.GetBool(
                RegistryHive.CurrentUser,
                RegistrySettingsCatalog.SuggestedContentSettings,
                true)
            &&
            RegistrySettingsEngine.GetBool(
                RegistryHive.CurrentUser,
                RegistrySettingsCatalog.SuggestedContentSettings2,
                true)
            &&
            RegistrySettingsEngine.GetBool(
                RegistryHive.CurrentUser,
                RegistrySettingsCatalog.SuggestedContentSettings3,
                true);

        IsSuggestedNotificationsEnabled =
            RegistrySettingsEngine.GetBool(
                RegistryHive.CurrentUser,
                RegistrySettingsCatalog.MicrosoftPromotionalNotifications,
                true);

        IsLockScreenTipsEnabled =
            RegistrySettingsEngine.GetBool(
                RegistryHive.CurrentUser,
                RegistrySettingsCatalog.LockScreenTipsEnabled,
                true);

        IsWindowsTipsEnabled =
            RegistrySettingsEngine.GetBool(
                RegistryHive.CurrentUser,
                RegistrySettingsCatalog.WindowsTipsEnabled,
                true);

        IsWindowsWelcomeExperienceEnabled =
            RegistrySettingsEngine.GetBool(
                RegistryHive.CurrentUser,
                RegistrySettingsCatalog.WelcomeExperienceEnabled,
                true);

        IsFinishSettingUpDeviceEnabled =
            RegistrySettingsEngine.GetBool(
                RegistryHive.CurrentUser,
                RegistrySettingsCatalog.ScoobeSystemSettingEnabled,
                true);
    }

    #endregion

    #region PrivacyLow

    [ObservableProperty]
    public partial bool IsApplicationTelemetryEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsTypingAndInkingDataCollectionEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsTypingAndInkingPersonalizationEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsApplicationLaunchTrackingEnabled { get; set; }

    partial void OnIsApplicationTelemetryEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.ApplicationTelemetryEnabled,
            value);
    }

    partial void OnIsTypingAndInkingDataCollectionEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.AllowLinguisticDataCollection,
            value);
    }

    partial void OnIsTypingAndInkingPersonalizationEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.RestrictImplicitTextCollection,
            !value);

        RegistrySettingsEngine.SetBool(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.RestrictImplicitInkCollection,
            !value);
    }

    partial void OnIsApplicationLaunchTrackingEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.TrackApplicationLaunches,
            value);
    }

    private void RefreshPrivacyLowProperties()
    {
        IsApplicationTelemetryEnabled =
            RegistrySettingsEngine.GetBool(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.ApplicationTelemetryEnabled,
                true);

        IsTypingAndInkingDataCollectionEnabled =
            RegistrySettingsEngine.GetBool(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.AllowLinguisticDataCollection,
                true);

        IsTypingAndInkingPersonalizationEnabled =
             !RegistrySettingsEngine.GetBool(
                 RegistryHive.CurrentUser,
                 RegistrySettingsCatalog.RestrictImplicitTextCollection,
                 false)
             &&
             !RegistrySettingsEngine.GetBool(
                 RegistryHive.CurrentUser,
                 RegistrySettingsCatalog.RestrictImplicitInkCollection,
                 false);

        IsApplicationLaunchTrackingEnabled =
            RegistrySettingsEngine.GetBool(
                RegistryHive.CurrentUser,
                RegistrySettingsCatalog.TrackApplicationLaunches,
                true);
    }

    #endregion

    #region PrivacyMedium

    [ObservableProperty]
    public partial bool IsWindowsCeipEnabled { get; set; }

    partial void OnIsWindowsCeipEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.WindowsCeipEnabled,
            value);
    }

    [ObservableProperty]
    public partial bool IsErrorReportingEnabled { get; set; }

    partial void OnIsErrorReportingEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.WindowsErrorReportingDisabled,
            !value);
    }

    [ObservableProperty]
    public partial bool IsOnlineSpeechRecognitionEnabled { get; set; }

    partial void OnIsOnlineSpeechRecognitionEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.OnlineSpeechRecognition,
            value);
    }

    [ObservableProperty]
    public partial bool IsLocationServicesEnabled { get; set; }

    partial void OnIsLocationServicesEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.LocationServices,
            value ? 0 : 1);
    }

    private void RefreshPrivacyMediumProperties()
    {
        IsWindowsCeipEnabled = RegistrySettingsEngine.GetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.WindowsCeipEnabled,
            true);
        IsErrorReportingEnabled = !RegistrySettingsEngine.GetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.WindowsErrorReportingDisabled,
            false);
        IsOnlineSpeechRecognitionEnabled = RegistrySettingsEngine.GetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.OnlineSpeechRecognition,
            true);
        IsLocationServicesEnabled = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.LocationServices,
            0) == 0;
    }

    #endregion

    #region PrivacyHigh

    [ObservableProperty] public partial int TelemetryLevel { get; set; }

    [ObservableProperty] public partial bool IsTailoredExperiencesEnabled { get; set; }

    [ObservableProperty] public partial bool IsAdvertisingIdEnabled { get; set; }

    [ObservableProperty] public partial bool IsActivityHistoryEnabled { get; set; }

    partial void OnTelemetryLevelChanged(int value)
    {
        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.AllowTelemetry,
            value switch
            {
                0 => 0,
                1 => 1,
                2 => 3,
                _ => 1
            });
    }

    partial void OnIsTailoredExperiencesEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.TailoredExperiences,
            value ? 0 : 1);
    }

    partial void OnIsAdvertisingIdEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.CurrentUser,
            RegistrySettingsCatalog.AdvertisingId,
            value);
    }

    partial void OnIsActivityHistoryEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.PublishUserActivities,
            value);
    }

    private void RefreshPrivacyHighProperties()
    {
        TelemetryLevel =
            RegistrySettingsEngine.GetValue<int>(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.AllowTelemetry,
                3) switch
            {
                0 => 0,
                1 => 1,
                3 => 2,
                _ => 1
            };

        IsTailoredExperiencesEnabled =
            RegistrySettingsEngine.GetValue<int>(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.TailoredExperiences,
                0) == 0;

        IsAdvertisingIdEnabled =
            RegistrySettingsEngine.GetBool(
                RegistryHive.CurrentUser,
                RegistrySettingsCatalog.AdvertisingId,
                true);

        IsActivityHistoryEnabled =
            RegistrySettingsEngine.GetBool(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.PublishUserActivities,
                true);
    }

    #endregion

    #region UAC

    [ObservableProperty] public partial int UacLevel { get; set; }

    partial void OnUacLevelChanged(int value)
    {
        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.EnableLUA,
            1);

        switch (value)
        {
            case 0: // Always notify
                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.ConsentPromptBehaviorAdmin,
                    2);

                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.PromptOnSecureDesktop,
                    1);
                break;

            case 1: // Notify me only when apps try to make changes
                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.ConsentPromptBehaviorAdmin,
                    5);

                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.PromptOnSecureDesktop,
                    1);
                break;

            case 2: // Notify me only when apps try to make changes (no secure desktop)
                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.ConsentPromptBehaviorAdmin,
                    5);

                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.PromptOnSecureDesktop,
                    0);
                break;

            case 3: // Never notify
                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.ConsentPromptBehaviorAdmin,
                    0);

                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.PromptOnSecureDesktop,
                    0);
                break;
        }
    }

    private void RefreshUacProperties()
    {
        int consent = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.ConsentPromptBehaviorAdmin,
            5);

        int secureDesktop = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.PromptOnSecureDesktop,
            1);

        UacLevel = (consent, secureDesktop) switch
        {
            (2, 1) => 0,
            (5, 1) => 1,
            (5, 0) => 2,
            (0, 0) => 3,
            _ => 1 // Windows default
        };
    }

    #endregion

    #region Developer mode

    [ObservableProperty] public partial bool DeveloperMode { get; set; }

    partial void OnDeveloperModeChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.AllowDevelopmentWithoutDevLicense,
            value);

        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.AllowAllTrustedApps,
            value);
    }

    private void RefreshDeveloperModeProperties()
    {
        DeveloperMode =
            RegistrySettingsEngine.GetBool(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.AllowDevelopmentWithoutDevLicense) &&
            RegistrySettingsEngine.GetBool(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.AllowAllTrustedApps);
    }

    #endregion

    #region Execution policies

    [ObservableProperty] public partial int PowerShellLevel { get; set; }

    [ObservableProperty] public partial bool IsScriptHostEnabled { get; set; }

    [ObservableProperty] public partial bool IsMarkOfTheWebEnabled { get; set; }

    partial void OnPowerShellLevelChanged(int value)
    {
        RegistrySettingsEngine.SetString(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.PowerShellExecutionPolicy,
            value switch
            {
                0 => "Restricted",
                1 => "AllSigned",
                2 => "RemoteSigned",
                3 => "Unrestricted",
                4 => "Bypass",
                _ => "Undefined"
            });
    }

    partial void OnIsScriptHostEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.WindowsScriptHostEnabled,
            value);
    }

    partial void OnIsMarkOfTheWebEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.SaveZoneInformation,
            value ? 2 : 1);
    }

    private void RefreshExecutionPolicies()
    {
        string? powerShellValue = RegistrySettingsEngine.GetValue<string>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.PowerShellExecutionPolicy,
            "RemoteSigned");
        PowerShellLevel = powerShellValue switch
        {
            "Restricted" => 0,
            "AllSigned" => 1,
            "RemoteSigned" => 2,
            "Unrestricted" => 3,
            "Bypass" => 4,
            _ => 0
        };

        IsScriptHostEnabled = RegistrySettingsEngine.GetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.WindowsScriptHostEnabled,
            true);

        IsMarkOfTheWebEnabled = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.SaveZoneInformation,
            2) == 2;
    }

    #endregion

    #region Sudo

    [ObservableProperty] public partial int SudoLevel { get; set; }

    partial void OnSudoLevelChanged(int value)
    {
        RegistrySettingsEngine.SetValue(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.EnableSudo,
            value switch
            {
                0 => 0,
                1 => 3,
                2 => 2,
                3 => 1,
                _ => 0
            });
    }

    private void RefreshSudoProperties()
    {
        int sudoValue = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.EnableSudo);
        SudoLevel = sudoValue switch
        {
            0 => 0,
            3 => 1,
            2 => 2,
            1 => 3,
            _ => 0
        };
    }

    #endregion

    #region "Smart"Screen

    [ObservableProperty] public partial bool IsSmartScreenEnabled { get; set; }

    partial void OnIsSmartScreenEnabledChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.EnableSmartScreen,
            value);
    }

    private void RefreshSmartScreenProperties()
    {
        IsSmartScreenEnabled = RegistrySettingsEngine.GetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.EnableSmartScreen,
            true);
    }

    #endregion

    #region Copilot

    [ObservableProperty] public partial bool CopilotInSettings { get; set; }

    [ObservableProperty] public partial bool CopilotInPaint { get; set; }

    [ObservableProperty] public partial bool CopilotInNotepad { get; set; }

    [ObservableProperty] public partial bool IsRecallOn { get; set; }

    [ObservableProperty] public partial bool IsRecallSnapshotsOn { get; set; }

    [ObservableProperty] public partial bool IsClickToDoOn { get; set; }

    partial void OnCopilotInSettingsChanged(bool value) 
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableSettingsAgent,
            !value);
    }

    partial void OnCopilotInPaintChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableCocreator,
            !value);

        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableGenerativeFill,
            !value);

        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableImageCreator,
            !value);
    }

    partial void OnCopilotInNotepadChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableAIFeaturesInNotepad,
            !value);
    }

    partial void OnIsRecallOnChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.AllowRecallEnablement,
            value);

        if (!value)
        {
            IsRecallSnapshotsOn = false;
        }
    }

    partial void OnIsRecallSnapshotsOnChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableAIDataAnalysis,
            !value);
    }

    partial void OnIsClickToDoOnChanged(bool value)
    {
        RegistrySettingsEngine.SetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableClickToDo,
            !value);
    }

    private void RefreshCopilotProperties()
    {
        CopilotInSettings = !RegistrySettingsEngine.GetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableSettingsAgent);

        CopilotInPaint = 
            !RegistrySettingsEngine.GetBool(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.DisableCocreator) &&
            !RegistrySettingsEngine.GetBool(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.DisableGenerativeFill) &&
            !RegistrySettingsEngine.GetBool(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.DisableImageCreator);

        CopilotInNotepad = !RegistrySettingsEngine.GetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableAIFeaturesInNotepad);

        IsRecallOn = RegistrySettingsEngine.GetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.AllowRecallEnablement);

        IsRecallSnapshotsOn = IsRecallOn &&
            !RegistrySettingsEngine.GetBool(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.DisableAIDataAnalysis);

        IsClickToDoOn = !RegistrySettingsEngine.GetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.DisableClickToDo);
    }

    #endregion

    #region Windows Update

    [ObservableProperty] public partial int WindowsUpdateState { get; set; }

    [ObservableProperty] public partial int WindowsUpdateConfig { get; set; }

    partial void OnWindowsUpdateStateChanged(int value)
    {
        switch (value)
        {
            case 0: // Default
                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.NoAutoUpdate,
                    0);

                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.AUOptions,
                    4);

                RegistrySettingsEngine.DeleteValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.SetDisableUXWUAccess);
                break;

            case 1: // Ask for consent
                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.NoAutoUpdate,
                    0);

                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.AUOptions,
                    2);

                RegistrySettingsEngine.DeleteValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.SetDisableUXWUAccess);
                break;

            case 2: // Manual
                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.NoAutoUpdate,
                    1);

                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.AUOptions,
                    1);

                RegistrySettingsEngine.DeleteValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.SetDisableUXWUAccess);
                break;

            case 3: // Disabled
                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.NoAutoUpdate,
                    1);

                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.AUOptions,
                    1);

                RegistrySettingsEngine.SetBool(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.SetDisableUXWUAccess,
                    true);
                break;
        }
    }

    partial void OnWindowsUpdateConfigChanged(int value)
    {
        switch (value)
        {
            case 0: // Get updates as soon as they're available
                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.SetAllowOptionalContent,
                    1);

                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.AllowOptionalContent,
                    1);

                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.ExcludeWUDriversInQualityUpdate,
                    0);
                break;

            case 1: // Default
                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.SetAllowOptionalContent,
                    1);

                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.AllowOptionalContent,
                    2);

                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.ExcludeWUDriversInQualityUpdate,
                    0);
                break;

            case 2: // Important updates only
                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.SetAllowOptionalContent,
                    1);

                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.AllowOptionalContent,
                    0);

                RegistrySettingsEngine.SetValue(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.ExcludeWUDriversInQualityUpdate,
                    1);
                break;
        }
    }

    private void RefreshWindowsUpdateProperties()
    {
        // WindowsUpdateState

        bool noUI = RegistrySettingsEngine.GetBool(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.SetDisableUXWUAccess);

        if (noUI)
        {
            WindowsUpdateState = 3; // Disabled
        }
        else
        {
            bool noAutoUpdate = RegistrySettingsEngine.GetBool(
                RegistryHive.LocalMachine,
                RegistrySettingsCatalog.NoAutoUpdate);

            if (noAutoUpdate)
            {
                WindowsUpdateState = 2; // Manual
            }
            else
            {
                int auOptions = RegistrySettingsEngine.GetValue<int>(
                    RegistryHive.LocalMachine,
                    RegistrySettingsCatalog.AUOptions);

                WindowsUpdateState = auOptions switch
                {
                    2 => 1, // Ask for consent
                    4 => 0, // Default
                    _ => 0  // Default fallback
                };
            }
        }

        // WindowsUpdateConfig

        int optionalContent = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.AllowOptionalContent);

        int excludeDrivers = RegistrySettingsEngine.GetValue<int>(
            RegistryHive.LocalMachine,
            RegistrySettingsCatalog.ExcludeWUDriversInQualityUpdate);

        WindowsUpdateConfig = (optionalContent, excludeDrivers) switch
        {
            (1, 0) => 0, // Get updates as soon as they're available
            (2, 0) => 1, // Default
            (0, 1) => 2, // Important updates only
            _ => 1       // Default fallback
        };
    }

    #endregion

    #region DMA

    [ObservableProperty] public partial bool IsEdgeUninstallable { get; set; }
    [ObservableProperty] public partial bool EdgeDefaultLock { get; set; }
    [ObservableProperty] public partial bool DefaultAppsExtraTypes { get; set; }
    [ObservableProperty] public partial bool XboxFullscreenExperience { get; set; }
    [ObservableProperty] public partial bool WidgetsDataRestriction { get; set; }
    [ObservableProperty] public partial bool ThirdPartyWidgetsDataRestriction { get; set; }
    [ObservableProperty] public partial bool SharedOddConsent { get; set; }
    [ObservableProperty] public partial bool WindowsCopilot { get; set; }
    [ObservableProperty] public partial bool AutomaticAppSignin { get; set; }
    [ObservableProperty] public partial bool FullscreenSetupPromotions { get; set; }
    [ObservableProperty] public partial bool SetupFlowPromotionalPages { get; set; }
    [ObservableProperty] public partial bool EdgePromotionOverrideDefaultBrowser { get; set; }
    [ObservableProperty] public partial bool CampaignSegmentTargeting { get; set; }
    [ObservableProperty] public partial bool PersonalizedOffers { get; set; }
    [ObservableProperty] public partial bool CopilotPwaPrepin { get; set; }
    [ObservableProperty] public partial bool AccountSyncConsent { get; set; }
    [ObservableProperty] public partial bool StartExperiencesUninstallable { get; set; }
    [ObservableProperty] public partial bool PrivacyUxModifiedLayout { get; set; }
    [ObservableProperty] public partial bool RecommendedActions { get; set; }
    [ObservableProperty] public partial bool StoreRegionSpecificOptions { get; set; }

    [ObservableProperty] public partial int DMAIndex { get; set; }

    partial void OnIsEdgeUninstallableChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.EDGE_UNINSTALLABLE, value);
        UpdateDMAIndexFromState();
    }

    // INVERTED
    partial void OnEdgeDefaultLockChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.EDGE_DEFAULT_LOCK, !value);
        UpdateDMAIndexFromState();
    }

    partial void OnDefaultAppsExtraTypesChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.DEFAULT_APPS_EXTRA_TYPES, value);
        UpdateDMAIndexFromState();
    }

    partial void OnXboxFullscreenExperienceChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.XBOX_FULLSCREEN_EXPERIENCE, value);
        UpdateDMAIndexFromState();
    }

    partial void OnWidgetsDataRestrictionChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.WIDGETS_DATA_RESTRICTION, value);
        UpdateDMAIndexFromState();
    }

    partial void OnThirdPartyWidgetsDataRestrictionChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.THIRD_PARTY_WIDGETS_DATA_RESTRICTION, value);
        UpdateDMAIndexFromState();
    }

    partial void OnSharedOddConsentChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.SHARED_ODD_CONSENT, value);
        UpdateDMAIndexFromState();
    }

    // INVERTED
    partial void OnWindowsCopilotChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.WINDOWS_COPILOT, !value);
        UpdateDMAIndexFromState();
    }

    // INVERTED
    partial void OnAutomaticAppSigninChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.AUTOMATIC_APP_SIGNIN, !value);
        UpdateDMAIndexFromState();
    }

    // INVERTED
    partial void OnFullscreenSetupPromotionsChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.FULLSCREEN_SETUP_PROMOTIONS, !value);
        UpdateDMAIndexFromState();
    }

    // INVERTED
    partial void OnSetupFlowPromotionalPagesChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.SETUP_FLOW_PROMOTIONAL_PAGES, !value);
        UpdateDMAIndexFromState();
    }

    // INVERTED
    partial void OnEdgePromotionOverrideDefaultBrowserChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.EDGE_PROMOTION_OVERRIDE_DEFAULT_BROWSER, !value);
        UpdateDMAIndexFromState();
    }

    // INVERTED
    partial void OnCampaignSegmentTargetingChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.CAMPAIGN_SEGMENT_TARGETING, !value);
        UpdateDMAIndexFromState();
    }

    // INVERTED
    partial void OnPersonalizedOffersChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.PERSONALIZED_OFFERS, !value);
        UpdateDMAIndexFromState();
    }

    // INVERTED
    partial void OnCopilotPwaPrepinChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.COPILOT_PWA_PREPIN, !value);
        UpdateDMAIndexFromState();
    }

    partial void OnAccountSyncConsentChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.ACCOUNT_SYNC_CONSENT, value);
        UpdateDMAIndexFromState();
    }

    partial void OnStartExperiencesUninstallableChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.START_EXPERIENCES_UNINSTALLABLE, value);
        UpdateDMAIndexFromState();
    }

    partial void OnPrivacyUxModifiedLayoutChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.PRIVACY_UX_MODIFIED_LAYOUT, value);
        UpdateDMAIndexFromState();
    }

    // INVERTED
    partial void OnRecommendedActionsChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.RECOMMENDED_ACTIONS, !value);
        UpdateDMAIndexFromState();
    }

    partial void OnStoreRegionSpecificOptionsChanged(bool value)
    {
        DMAService.ToggleDmaFeature(DMAService.STORE_REGION_SPECIFIC_OPTIONS, value);
        UpdateDMAIndexFromState();
    }

    partial void OnDMAIndexChanged(int value)
    {
        int calculatedIndex = GetCalculatedIndex();

        if (value != calculatedIndex)
        {
            switch (value)
            {
                case 0: // Disable / Remove all
                    SetAllFeatures(false);
                    break;
                case 2: // Enable / Apply all
                    SetAllFeatures(true);
                    break;
            }

            RefreshDMAProperties();
        }
    }

    private void SetAllFeatures(bool state)
    {
        DMAService.ToggleDmaFeature(DMAService.EDGE_UNINSTALLABLE, state);
        DMAService.ToggleDmaFeature(DMAService.EDGE_DEFAULT_LOCK, !state);
        DMAService.ToggleDmaFeature(DMAService.DEFAULT_APPS_EXTRA_TYPES, state);
        DMAService.ToggleDmaFeature(DMAService.XBOX_FULLSCREEN_EXPERIENCE, state);
        DMAService.ToggleDmaFeature(DMAService.WIDGETS_DATA_RESTRICTION, state);
        DMAService.ToggleDmaFeature(DMAService.THIRD_PARTY_WIDGETS_DATA_RESTRICTION, state);
        DMAService.ToggleDmaFeature(DMAService.SHARED_ODD_CONSENT, state);
        DMAService.ToggleDmaFeature(DMAService.WINDOWS_COPILOT, !state);
        DMAService.ToggleDmaFeature(DMAService.AUTOMATIC_APP_SIGNIN, !state);
        DMAService.ToggleDmaFeature(DMAService.FULLSCREEN_SETUP_PROMOTIONS, !state);
        DMAService.ToggleDmaFeature(DMAService.SETUP_FLOW_PROMOTIONAL_PAGES, !state);
        DMAService.ToggleDmaFeature(DMAService.EDGE_PROMOTION_OVERRIDE_DEFAULT_BROWSER, !state);
        DMAService.ToggleDmaFeature(DMAService.CAMPAIGN_SEGMENT_TARGETING, !state);
        DMAService.ToggleDmaFeature(DMAService.PERSONALIZED_OFFERS, !state);
        DMAService.ToggleDmaFeature(DMAService.COPILOT_PWA_PREPIN, !state);
        DMAService.ToggleDmaFeature(DMAService.ACCOUNT_SYNC_CONSENT, state);
        DMAService.ToggleDmaFeature(DMAService.START_EXPERIENCES_UNINSTALLABLE, state);
        DMAService.ToggleDmaFeature(DMAService.PRIVACY_UX_MODIFIED_LAYOUT, state);
        DMAService.ToggleDmaFeature(DMAService.RECOMMENDED_ACTIONS, !state);
        DMAService.ToggleDmaFeature(DMAService.STORE_REGION_SPECIFIC_OPTIONS, state);
    }

    public void RefreshDMAProperties()
    {
        IsEdgeUninstallable = DMAService.CheckIsDmaFeatureEnabled(DMAService.EDGE_UNINSTALLABLE);
        EdgeDefaultLock = !DMAService.CheckIsDmaFeatureEnabled(DMAService.EDGE_DEFAULT_LOCK);
        DefaultAppsExtraTypes = DMAService.CheckIsDmaFeatureEnabled(DMAService.DEFAULT_APPS_EXTRA_TYPES);
        XboxFullscreenExperience = DMAService.CheckIsDmaFeatureEnabled(DMAService.XBOX_FULLSCREEN_EXPERIENCE);
        WidgetsDataRestriction = DMAService.CheckIsDmaFeatureEnabled(DMAService.WIDGETS_DATA_RESTRICTION);
        ThirdPartyWidgetsDataRestriction = DMAService.CheckIsDmaFeatureEnabled(DMAService.THIRD_PARTY_WIDGETS_DATA_RESTRICTION);
        SharedOddConsent = DMAService.CheckIsDmaFeatureEnabled(DMAService.SHARED_ODD_CONSENT);
        WindowsCopilot = !DMAService.CheckIsDmaFeatureEnabled(DMAService.WINDOWS_COPILOT);
        AutomaticAppSignin = !DMAService.CheckIsDmaFeatureEnabled(DMAService.AUTOMATIC_APP_SIGNIN);
        FullscreenSetupPromotions = !DMAService.CheckIsDmaFeatureEnabled(DMAService.FULLSCREEN_SETUP_PROMOTIONS);
        SetupFlowPromotionalPages = !DMAService.CheckIsDmaFeatureEnabled(DMAService.SETUP_FLOW_PROMOTIONAL_PAGES);
        EdgePromotionOverrideDefaultBrowser = !DMAService.CheckIsDmaFeatureEnabled(DMAService.EDGE_PROMOTION_OVERRIDE_DEFAULT_BROWSER);
        CampaignSegmentTargeting = !DMAService.CheckIsDmaFeatureEnabled(DMAService.CAMPAIGN_SEGMENT_TARGETING);
        PersonalizedOffers = !DMAService.CheckIsDmaFeatureEnabled(DMAService.PERSONALIZED_OFFERS);
        CopilotPwaPrepin = !DMAService.CheckIsDmaFeatureEnabled(DMAService.COPILOT_PWA_PREPIN);
        AccountSyncConsent = DMAService.CheckIsDmaFeatureEnabled(DMAService.ACCOUNT_SYNC_CONSENT);
        StartExperiencesUninstallable = DMAService.CheckIsDmaFeatureEnabled(DMAService.START_EXPERIENCES_UNINSTALLABLE);
        PrivacyUxModifiedLayout = DMAService.CheckIsDmaFeatureEnabled(DMAService.PRIVACY_UX_MODIFIED_LAYOUT);
        RecommendedActions = !DMAService.CheckIsDmaFeatureEnabled(DMAService.RECOMMENDED_ACTIONS);
        StoreRegionSpecificOptions = DMAService.CheckIsDmaFeatureEnabled(DMAService.STORE_REGION_SPECIFIC_OPTIONS);

        UpdateDMAIndexFromState();
    }

    private void UpdateDMAIndexFromState()
    {
        DMAIndex = GetCalculatedIndex();
    }

    private int GetCalculatedIndex()
    {
        bool allOff = !IsEdgeUninstallable && !EdgeDefaultLock && !DefaultAppsExtraTypes &&
                      !XboxFullscreenExperience && !WidgetsDataRestriction && !ThirdPartyWidgetsDataRestriction &&
                      !SharedOddConsent && !WindowsCopilot && !AutomaticAppSignin &&
                      !FullscreenSetupPromotions && !SetupFlowPromotionalPages && !EdgePromotionOverrideDefaultBrowser &&
                      !CampaignSegmentTargeting && !PersonalizedOffers && !CopilotPwaPrepin &&
                      !AccountSyncConsent && !StartExperiencesUninstallable && !PrivacyUxModifiedLayout &&
                      !RecommendedActions && !StoreRegionSpecificOptions;

        if (allOff) return 0;

        bool allOn = IsEdgeUninstallable && EdgeDefaultLock && DefaultAppsExtraTypes &&
                     XboxFullscreenExperience && WidgetsDataRestriction && ThirdPartyWidgetsDataRestriction &&
                     SharedOddConsent && WindowsCopilot && AutomaticAppSignin &&
                     FullscreenSetupPromotions && SetupFlowPromotionalPages && EdgePromotionOverrideDefaultBrowser &&
                     CampaignSegmentTargeting && PersonalizedOffers && CopilotPwaPrepin &&
                     AccountSyncConsent && StartExperiencesUninstallable && PrivacyUxModifiedLayout &&
                     RecommendedActions && StoreRegionSpecificOptions;

        if (allOn) return 2;

        return 1;
    }

    #endregion

    #region UCPD

    private const string SubKey = @"SYSTEM\CurrentControlSet\Services\UCPD";
    private const string ValueName = "Start";
    private const string TaskPath = @"\Microsoft\Windows\AppxDeploymentClient\UCPD velocity";

    private const uint ServiceStartAutomatic = 2; // Enforcement active
    private const uint ServiceStartDisabled = 4;  // Restrictions relaxed

    [ObservableProperty]
    public partial bool IsUcpdEnabled { get; set; }

    /// <summary>
    /// Generator hook for property changes.
    /// </summary>
    partial void OnIsUcpdEnabledChanged(bool value)
    {
        // Target state: if IsUcpdEnabled == true, UCPD is enabled -> disable parameter is false
        bool shouldDisableUcpd = !value;
        Task.Run(() => SetUcpdRestriction(disable: shouldDisableUcpd));
    }

    public void RefreshUcpdProperties()
    {
        IsUcpdEnabled = !IsUcpdDisabled();
    }

    /// <summary>
    /// Reads registry service start type using non-elevated KEY_QUERY_VALUE.
    /// </summary>
    public static unsafe bool IsUcpdDisabled()
    {
        fixed (char* lpSubKey = SubKey)
        fixed (char* lpValueName = ValueName)
        {
            HKEY hKey;
            int status = RegOpenKeyExW(
                HKEY_LOCAL_MACHINE,
                (char*)lpSubKey,
                0,
                KEY_QUERY_VALUE,
                &hKey
            );

            if (status != 0) return false;

            uint value = 0;
            uint type = 0;
            uint size = sizeof(uint);

            status = RegQueryValueExW(
                hKey,
                (char*)lpValueName,
                null,
                &type,
                (byte*)&value,
                &size
            );

            RegCloseKey(hKey);

            return status == 0 && value == ServiceStartDisabled;
        }
    }

    /// <summary>
    /// Updates both Registry and Task Scheduler state via TerraFX COM.
    /// </summary>
    public static unsafe int SetUcpdRestriction(bool disable)
    {
        uint serviceStartValue = disable ? ServiceStartDisabled : ServiceStartAutomatic;

        // 1. Update Registry Service
        fixed (char* lpSubKey = SubKey)
        fixed (char* lpValueName = ValueName)
        {
            HKEY hKey;
            int status = RegOpenKeyExW(
                HKEY_LOCAL_MACHINE,
                (char*)lpSubKey,
                0,
                KEY_SET_VALUE,
                &hKey
            );

            if (status != 0) return status;

            status = RegSetValueExW(
                hKey,
                (char*)lpValueName,
                0,
                REG_DWORD,
                (byte*)&serviceStartValue,
                sizeof(uint)
            );

            RegCloseKey(hKey);

            if (status != 0) return status;
        }

        // 2. Update Task Scheduler directly via COM
        ExecuteTaskOperationAsync(disableIfFound: true, disableState: disable);

        return 0;
    }

    public static async Task<bool> ExecuteTaskOperationAsync(bool disableIfFound, bool disableState = true)
    {
        // Offload to MTA ThreadPool thread to prevent WinUI STA COM crashes & UI hangs
        return await Task.Run(() => ExecuteTaskOperation(disableIfFound, out _, disableState));
    }

    public static unsafe bool ExecuteTaskOperation(bool disableIfFound, out bool isDisabled, bool disableState = true)
    {
        isDisabled = false;

        ManagedPtr<Guid> clsid = new Guid("0F87369F-A4E5-4CFC-BD3E-73E6154572DD");

        try
        {
            // 1. ManagedPtr is ONLY for real COM interfaces
            using ComPtr<ITaskService> pService = default;
            using ComPtr<ITaskFolder> pRootFolder = default;
            using ComPtr<IRegisteredTask> pTask = default;

            Guid iidService = *ITaskService.IID;

            HRESULT hr = CoCreateInstance(clsid, null, (uint)CLSCTX.CLSCTX_INPROC_SERVER, &iidService, (void**)pService.GetAddressOf());
            if (FAILED(hr)) return false;

            VARIANT vNull = default;
            hr = pService.Get()->Connect(vNull, vNull, vNull, vNull);
            if (FAILED(hr)) return false;

            fixed (char* pPath = @"\")
            {
                hr = pService.Get()->GetFolder((ushort*)pPath, pRootFolder.GetAddressOf());
                if (FAILED(hr)) return false;
            }

            // 3. NO leading backslash for GetTask!
            fixed (char* pTaskPath = @"Microsoft\Windows\AppxDeploymentClient\UCPD velocity")
            {
                hr = pRootFolder.Get()->GetTask((ushort*)pTaskPath, pTask.GetAddressOf());
                if (FAILED(hr)) return false;
            }

            // 4. Query state (16-bit short)
            short isEnabledVariant = 0;
            hr = pTask.Get()->get_Enabled(&isEnabledVariant);
            if (FAILED(hr)) return false;

            isDisabled = (isEnabledVariant == 0);

            if (disableIfFound)
            {
                short newEnabledState = disableState ? (short)0 : (short)(-1);
                hr = pTask.Get()->put_Enabled(newEnabledState);
                if (FAILED(hr)) return false;

                isDisabled = disableState;
            }

            return true;
        }
        finally
        {
            clsid.Dispose();
        }
    }

    #endregion
}