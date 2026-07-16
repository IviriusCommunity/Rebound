// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Rebound.Cleanup.Helpers;
using Rebound.Cleanup.Items;
using Rebound.Core.Environment;
using Rebound.Core.Native.Wrappers;
using Rebound.Core.Settings;
using Rebound.Core.SystemInformation.Software;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using Windows.System;
using static TerraFX.Interop.Windows.S;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Cleanup.ViewModels;

internal partial class MainViewModel : ObservableObject
{
    public ObservableCollection<DriveComboBoxItem> DriveItems { get; } = DriveHelper.GetDriveItems();

    public ObservableCollection<CleanItem> CleanItems { get; set; } = [];

    public bool IsRunningAsAdmin { get; } = ApplicationEnvironment.IsRunningAsAdmin();

    [ObservableProperty] public partial int FilesCount { get; set; }

    [ObservableProperty] public partial long FilesSize { get; set; } = 0;

    [ObservableProperty] public partial bool IsLoading { get; set; } = false;

    [ObservableProperty] public partial bool? IsEverythingSelected { get; set; }

    [ObservableProperty] public partial bool CanItemsBeClicked { get; set; } = true;

    [ObservableProperty] public partial int SelectedDriveIndex { get; set; }

    partial void OnIsEverythingSelectedChanged(bool? oldValue, bool? newValue)
    {
        switch (newValue)
        {
            case true:
                foreach (var item in CleanItems)
                {
                    item.IsChecked = true;
                }
                break;
            case false:
                foreach (var item in CleanItems)
                {
                    item.IsChecked = false;
                }
                break;
        }
    }

    async partial void OnSelectedDriveIndexChanged(int oldValue, int newValue)
    {
        SettingsManager.SetValue("SelectedDriveIndex", "cleanmgr", newValue);
        await RefreshCleanupListAsync(DriveItems[SelectedDriveIndex]);
    }

    public MainViewModel()
    {
        SelectedDriveIndex = SettingsManager.GetValue("SelectedDriveIndex", "cleanmgr", 0);
    }

    [RelayCommand]
    public static void DeleteOldRestorePointsAndShadowCopies()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "vssadmin",
                Arguments = $"delete shadows /for={WindowsInformation.GetWindowsInstallationDrivePath()[..1]}: /oldest",
                CreateNoWindow = true,
                UseShellExecute = false
            });

            Process.Start(new ProcessStartInfo
            {
                FileName = "wmic",
                Arguments = "shadowcopy delete",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch
        {

        }
    }

    [RelayCommand]
    public static void LaunchDefrag()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "dfrgui.exe",
            UseShellExecute = true
        });
    }

    [RelayCommand]
    public async Task RefreshCleanupListAsync(DriveComboBoxItem selectedItem)
    {
        if (selectedItem == null)
            return;

        // Reset the list to default
        CleanItems.Clear();

        // Store everything on the main thread
        var discoveredItems = new List<CleanItem>();

        // Capture these here for thread safety just in case
        var isSystemDrive = selectedItem.DrivePath == "C:\\";
        var isRunningAsAdmin = IsRunningAsAdmin;

        // Offload all heavy I/O stuff to a background thread
        await Task.Run(async () =>
        {
            // Helper to instantiate and refresh items on the background thread
            async Task AddItem(
                string name,
                CleanTarget[] targets,
                string image,
                string description,
                string id,
                string targetLaunchPath,
                bool isChecked = false)
            {
                var item = new CleanItem(isChecked)
                {
                    Name = name,
                    CleanTargets = targets,
                    Description = description,
                    ImagePath = image,
                    ItemID = id,
                    LaunchTargetPath = targetLaunchPath
                };
                lock (discoveredItems)
                {
                    discoveredItems.Add(item);
                }
            }

            // ---------------
            // Any disk, admin
            // ---------------
            if (isRunningAsAdmin)
            {
                // -----------
                // Recycle Bin
                // -----------
                await AddItem(
                    "Recycle Bin (All Users)",
                    [new CleanTarget()
                    {
                        Path = Path.Combine(selectedItem.DrivePath, "$Recycle.Bin"),

                        // Custom deletion action for Admin mode
                        CustomDeleteAction = async (target, cancellationToken) =>
                        {
                            await Task.Run(() =>
                            {
                                var recycleBinRootPath = Path.Combine(selectedItem.DrivePath, "$Recycle.Bin");
                                if (!Directory.Exists(recycleBinRootPath)) 
                                    return;

                                var rootDirectory = new DirectoryInfo(recycleBinRootPath);

                                // Loop through all SID folders (e.g., S-1-5-21-...) belonging to different users
                                foreach (var subDirectory in rootDirectory.EnumerateDirectories())
                                {
                                    try
                                    {
                                        // Strip hidden/system flags so Windows allows the deletion loop
                                        subDirectory.Attributes = FileAttributes.Normal;
                
                                        // Clear out all deleted file fragments and metadata indexes inside
                                        foreach (var file in subDirectory.EnumerateFiles("*", SearchOption.AllDirectories))
                                        {
                                            try
                                            {
                                                file.Attributes = FileAttributes.Normal;
                                                file.Delete();
                                            }
                                            catch (IOException) { /* File is currently locked/active */ }
                                            catch (UnauthorizedAccessException) { /* Bad permissions */ }
                                        }

                                        // Delete the user's specific subfolder container
                                        subDirectory.Delete(recursive: true);
                                    }
                                    catch (Exception)
                                    {
                                        // If a specific user's trash folder is totally locked, skip it and keep moving
                                    }
                                }
                            }, cancellationToken).ConfigureAwait(false);
                        },

                        // Custom enumerate action for Admin mode
                        CustomEnumerateAction = async (target, cancellationToken) =>
                        {
                            return await Task.Run(() =>
                            {
                                var recycleBinRootPath = Path.Combine(selectedItem.DrivePath, "$Recycle.Bin");
                                if (!Directory.Exists(recycleBinRootPath)) return (0L, 0);

                                long totalSizeBytes = 0;
                                int totalFiles = 0;
                                var rootDirectory = new DirectoryInfo(recycleBinRootPath);

                                foreach (var subDirectory in rootDirectory.EnumerateDirectories())
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    try
                                    {
                                        foreach (var file in subDirectory.EnumerateFiles("*", SearchOption.AllDirectories))
                                        {
                                            try
                                            {
                                                totalSizeBytes += file.Length;
                                                totalFiles++;
                                            }
                                            catch (FileNotFoundException) { /* Gone during scan */ }
                                            catch (UnauthorizedAccessException) { /* Hidden system file lock */ }
                                        }
                                    }
                                    catch (Exception) { /* Skip unreadable user SID directories */ }
                                }

                                return (totalSizeBytes, totalFiles);
                            }, cancellationToken).ConfigureAwait(false);
                        }
                    }],
                    "ms-appx:///Assets/CleanupItems/RecycleBin.ico",
                    "The Recycle Bin stores files and folders that you've deleted from your computer. These items are not permanently removed until you empty the Recycle Bin. You can recover deleted items from here, but deleting them permanently frees up disk space.",
                    "TRASH",
                    "shell:RecycleBinFolder",
                    true).ConfigureAwait(false);
            }

            // -------------------
            // Any disk, non-admin
            // -------------------
            else
            {
                // -----------
                // Recycle Bin
                // -----------
                await AddItem(
                    "Recycle Bin (Current User)",
                    [new CleanTarget()
                    {
                        Path = selectedItem.DrivePath,

                        // Custom deletion action for Non-Admin mode using standard Shell API
                        CustomDeleteAction = async (target, cancellationToken) =>
                        {
                            await Task.Run(() =>
                            {
                                uint flags = SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND;
                                
                                // drivePath must be formatted like "C:\"
                                using ManagedPtr<char> drivePathPtr = selectedItem.DrivePath;

                                // This clears the current user's partition on that specific drive
                                unsafe { SHEmptyRecycleBinW(HWND.NULL, drivePathPtr, flags); }
                            }, cancellationToken).ConfigureAwait(false);
                        },

                        // Custom enumerate action for Non-Admin mode
                        CustomEnumerateAction = async (target, cancellationToken) =>
                        {
                            return await Task.Run(() =>
                            {
                                unsafe
                                {
                                    // Setup the info structure sizes
                                    var rbInfo = new SHQUERYRBINFO { cbSize = (uint)sizeof(SHQUERYRBINFO) };

                                    using ManagedPtr<char> drivePathPtr = selectedItem.DrivePath;

                                    // Native query invoke
                                    int result = SHQueryRecycleBinW(drivePathPtr, &rbInfo);
                                    if (result == S_OK)
                                    {
                                        return (rbInfo.i64Size, (int)rbInfo.i64NumItems);
                                    }
                                }

                                return (0L, 0);
                            }, cancellationToken).ConfigureAwait(false);
                        }
                    }],
                    "ms-appx:///Assets/CleanupItems/RecycleBin.ico",
                    "The Recycle Bin stores files and folders that you've deleted from your computer. These items are not permanently removed until you empty the Recycle Bin. You can recover deleted items from here, but deleting them permanently frees up disk space.",
                    "TRASH",
                    "shell:RecycleBinFolder",
                    true).ConfigureAwait(false);
            }

            // -----------------------
            // System drive, non-admin
            // -----------------------
            if (isSystemDrive)
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                var windowsRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                var systemRoot = WindowsInformation.GetWindowsInstallationDrivePath();

                // ----
                // Temp
                // ----
                string tempPath = 
                    Environment.GetEnvironmentVariable("TEMP")
                    ?? Path.Combine(localAppData, "Temp");

                await AddItem(
                    "Temporary Files (Current User)",
                    [new() { Path = tempPath }],
                    "ms-appx:///Assets/CleanupItems/TemporaryFiles.ico",
                    "Stores temporary data created by applications and the system during normal operation.",
                    "APPDATA_TEMP",
                    tempPath
                ).ConfigureAwait(false);

                // ------------
                // Recent files
                // ------------
                string recentFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
                if (string.IsNullOrEmpty(recentFilesPath))
                    recentFilesPath = Path.Combine(appData, "Microsoft", "Windows", "Recent");

                await AddItem(
                    "Recent Files (Current User)",
                    [new() { Path = recentFilesPath }],
                    "ms-appx:///Assets/CleanupItems/RecentFiles.ico",
                    "Tracks recently opened files and documents for quick access in File Explorer and applications.",
                    "APPDATA_MS_WIN_RECENT",
                    recentFilesPath).ConfigureAwait(false);

                // -------------------------------------
                // INET Cache (Temporary Internet Files)
                // -------------------------------------
                string inetCachePath = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);

                if (string.IsNullOrEmpty(inetCachePath))
                    inetCachePath = Path.Combine(localAppData, "Microsoft", "Windows", "INetCache");

                await AddItem(
                    "Temporary Internet Files",
                    [new() { Path = inetCachePath }],
                    "ms-appx:///Assets/CleanupItems/TemporaryInternetFiles.ico",
                    "(Obsolete) Contains cached web content used by Internet Explorer and other legacy components.",
                    "LOCALAPPDATA_MS_WIN_INETCACHE", inetCachePath).ConfigureAwait(false);

                // --------------
                // Explorer cache
                // --------------
                string explorerCachePath = Path.Combine(localAppData, "Microsoft", "Windows", "Explorer");

                await AddItem(
                    "Thumbnails",
                    [new CleanTarget()
                    {
                        Path = explorerCachePath,

                        // Thumbnail dbs live strictly in the root of this folder
                        Depth = SearchDepth.TopDirectoryOnly, 

                        // Match only thumbcache and iconcache files, ignoring explorer configuration registries
                        FileFilter = (file) =>
                        {
                            string name = file.Name.ToUpperInvariant();
                            return (name.StartsWith("THUMBCACHE_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("ICONCACHE_", StringComparison.OrdinalIgnoreCase))
                                && name.EndsWith(".DB", StringComparison.OrdinalIgnoreCase);
                        },

                        // Custom Delete Action (Explorer locks these files so they need special handling not to throw)
                        CustomDeleteAction = async (target, cancellationToken) =>
                        {
                            await Task.Run(() =>
                            {
                                if (!Directory.Exists(target.Path)) return;

                                var dirInfo = new DirectoryInfo(target.Path);

                                foreach (FileInfo file in dirInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    // Apply the filter so it doesn't touch other Explorer files
                                    if (target.FileFilter(file))
                                    {
                                        try
                                        {
                                            // Strip readonly/system attributes just in case
                                            if (file.Attributes != FileAttributes.Normal)
                                            {
                                                file.Attributes = FileAttributes.Normal;
                                            }

                                            file.Delete();
                                        }
                                        catch (IOException)
                                        {
                                            // File is locked by Explorer for reasons™️
                                        }
                                        catch (UnauthorizedAccessException) { /* Insufficient permissions */ }
                                    }
                                }
                            }, cancellationToken).ConfigureAwait(false);
                        }
                    }],
                    "ms-appx:///Assets/CleanupItems/Thumbnails.ico",
                    "Contains cached thumbnail previews for images, videos, and documents.",
                    "MS_WIN_EXPLORER_THUMBNAILS",
                    explorerCachePath).ConfigureAwait(false);

                // ------------------
                // WER (Current User)
                // ------------------
                string userWerPath = Path.Combine(localAppData, "Microsoft", "Windows", "WER");

                var currentUserWerTargets = new[]
                {
                    new CleanTarget() { Path = Path.Combine(userWerPath, "ReportQueue"), Depth = SearchDepth.AllDirectories },
                    new CleanTarget() { Path = Path.Combine(userWerPath, "ReportArchive"), Depth = SearchDepth.AllDirectories }
                };

                await AddItem(
                    "Windows Error Reporting (Current User)",
                    currentUserWerTargets,
                    "ms-appx:///Assets/CleanupItems/WindowsErrorReporting.ico",
                    "Stores diagnostic reports generated when applications or the system encounter errors.",
                    "WER_CURRENTUSER",
                    userWerPath
                    ).ConfigureAwait(false);

                // ---------
                // Downloads
                // ---------
                Guid downloadsGuid = new Guid("374DE290-123F-4565-9164-39C4925E467B");
                string downloadsPath;

                unsafe
                {
                    ManagedPtr<char> pNativePath = default;

                    int hresult = SHGetKnownFolderPath(
#pragma warning disable CS9123 // The '&' operator should not be used on parameters or local variables in async methods.
                        &downloadsGuid,
#pragma warning restore CS9123 // The '&' operator should not be used on parameters or local variables in async methods.
                        0, 
                        HANDLE.NULL, 
                        (char**)pNativePath.ObjectPointerPointer);

                    if (hresult == 0 && pNativePath != NULL)
                    {
                        try
                        {
                            downloadsPath = pNativePath.ToStringValue();
                        }
                        finally
                        {
                            Marshal.FreeCoTaskMem(pNativePath);
                        }
                    }

                    // Emergency fallback if the Win32 API call fails
                    downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                }

                await AddItem(
                    "Downloads Folder (Current User)",
                    [new() { Path = downloadsPath }],
                    "ms-appx:///Assets/CleanupItems/Downloads.ico",
                    "Default location for files downloaded from the internet or transferred from other devices.",
                    "DOWNLOADS",
                    downloadsPath).ConfigureAwait(false);

                // --------------------
                // DirectX Shader Cache
                // --------------------
                var shaderTargets = new List<CleanTarget>();

                string d3dCache = Path.Combine(localAppData, "D3DSCache");
                if (Directory.Exists(d3dCache))
                    shaderTargets.Add(new CleanTarget() { Path = d3dCache, Depth = SearchDepth.AllDirectories });

                string dx12Cache = Path.Combine(localAppData, "Microsoft", "DirectX");
                if (Directory.Exists(dx12Cache))
                    shaderTargets.Add(new CleanTarget() { Path = dx12Cache, Depth = SearchDepth.AllDirectories });

                string nvidiaCache = Path.Combine(localAppData, "NVIDIA", "DXCache");
                if (Directory.Exists(nvidiaCache))
                    shaderTargets.Add(new CleanTarget() { Path = nvidiaCache, Depth = SearchDepth.AllDirectories });

                string amdCache = Path.Combine(localAppData, "AMD", "DxCache");
                if (Directory.Exists(amdCache))
                    shaderTargets.Add(new CleanTarget() { Path = amdCache, Depth = SearchDepth.AllDirectories });

                await AddItem(
                    "DirectX Shader Cache",
                    [..shaderTargets],
                    "ms-appx:///Assets/CleanupItems/DirectX.ico",
                    "Holds precompiled shader files used by graphics drivers and DirectX to enhance rendering performance and reduce game stutter.",
                    "DIRECTX_SHADER_CACHE",
                    dx12Cache,
                    false).ConfigureAwait(false);

                // --------
                // Browsers
                // --------
                var eng = new BrowserScannerEngine();
                var browsers = await eng.EnumerateBrowsersAsync().ConfigureAwait(false);
                foreach (var browser in browsers)
                    await AddItem(
                        browser.Name, 
                        browser.CleanTargets,
                        browser.ImagePath, 
                        browser.Description,
                        browser.ItemID,
                        string.Empty).ConfigureAwait(false);

                // -------------------
                // System drive, admin
                // -------------------
                if (isRunningAsAdmin)
                {
                    // ------------------------
                    // Downloaded Program Files
                    // ------------------------
                    string dpfPath = Path.Combine(windowsRoot, "Downloaded Program Files");

                    var dpfTargets = Directory.Exists(dpfPath)
                        ? [ new CleanTarget() { Path = dpfPath, Depth = SearchDepth.TopDirectoryOnly } ]
                        : Array.Empty<CleanTarget>();

                    await AddItem(
                        "Downloaded Program Files",
                        dpfTargets,
                        "ms-appx:///Assets/CleanupItems/DownloadedProgramFiles.ico",
                        "(Obsolete) Stores legacy ActiveX controls and Java applets downloaded from the internet in older versions of Windows.",
                        "DOWNLOADED_PROGRAM_FILES",
                        dpfPath,
                        true).ConfigureAwait(false);

                    // ------------
                    // WER (System)
                    // ------------
                    string systemWerPath = Path.Combine(programData, "Microsoft", "Windows", "WER");

                    var systemWerTargets = new List<CleanTarget>();
                    string sysQueue = Path.Combine(systemWerPath, "ReportQueue");
                    string sysArchive = Path.Combine(systemWerPath, "ReportArchive");

                    if (Directory.Exists(sysQueue))
                        systemWerTargets.Add(new CleanTarget() { Path = sysQueue, Depth = SearchDepth.AllDirectories });

                    if (Directory.Exists(sysArchive))
                        systemWerTargets.Add(new CleanTarget() { Path = sysArchive, Depth = SearchDepth.AllDirectories });

                    await AddItem(
                        "System Created Windows Error Reporting",
                        [..systemWerTargets],
                        "ms-appx:///Assets/CleanupItems/WindowsErrorReporting.ico",
                        "Stores system-wide diagnostic reports and memory dumps generated when background services or the operating system crash.",
                        "WER_SYSTEM",
                        systemWerPath,
                        false).ConfigureAwait(false);

                    // --------
                    // Prefetch
                    // --------
                    string prefetchPath = Path.Combine(windowsRoot, "Prefetch");

                    var prefetchTargets = Directory.Exists(prefetchPath)
                        ? [ new CleanTarget() { Path = prefetchPath, Depth = SearchDepth.TopDirectoryOnly } ]
                        : Array.Empty<CleanTarget>();

                    await AddItem(
                        "Prefetch",
                        prefetchTargets,
                        "ms-appx:///Assets/CleanupItems/Prefetch.ico",
                        "Stores application execution history. Cleaning this can free up space, but may temporarily slow down application launch times while Windows rebuilds the cache.",
                        "PREFETCH",
                        prefetchPath,
                        false).ConfigureAwait(false);

                    // ---------------------
                    // Delivery Optimization
                    // ---------------------
                    string wdoCachePath = Path.Combine(programData, "Microsoft", "Network", "Downloader");

                    var wdoTargets = Directory.Exists(wdoCachePath)
                        ? [ new CleanTarget() { Path = wdoCachePath, Depth = SearchDepth.AllDirectories } ]
                        : Array.Empty<CleanTarget>();

                    await AddItem(
                        "Delivery Optimization Cache",
                        wdoTargets,
                        "ms-appx:///Assets/CleanupItems/DeliveryOptimization.ico",
                        "Holds downloaded update files used for peer-to-peer delivery across local network devices to save internet bandwidth.",
                        "DELIVERY_OPTIMIZATION_CACHE",
                        wdoCachePath,
                        false).ConfigureAwait(false);

                    // -----------------------------
                    // Windows Update Download Cache
                    // -----------------------------
                    string updateStagingPath = Path.Combine(windowsRoot, "SoftwareDistribution", "Download");

                    var updateTargets = Directory.Exists(updateStagingPath)
                        ? [ new CleanTarget() { Path = updateStagingPath, Depth = SearchDepth.AllDirectories } ]
                        : Array.Empty<CleanTarget>();

                    await AddItem(
                        "Windows Update Download Cache",
                        updateTargets,
                        "ms-appx:///Assets/CleanupItems/WindowsUpdate.ico",
                        "Contains downloaded installation files for Windows Updates. It is safe to clear this folder to free up space after updates have been successfully installed.",
                        "WINDOWS_UPDATE_DOWNLOADS",
                        updateStagingPath,
                        false).ConfigureAwait(false);

                    // -------------------
                    // Windows Update Logs
                    // -------------------
                    string winUpdateLogPath = Path.Combine(windowsRoot, "Logs", "WindowsUpdate");

                    var logTargets = Directory.Exists(winUpdateLogPath)
                        ? [ new CleanTarget() { Path = winUpdateLogPath, Depth = SearchDepth.TopDirectoryOnly } ]
                        : Array.Empty<CleanTarget>();

                    await AddItem(
                        "Windows Update Logs",
                        logTargets,
                        "ms-appx:///Assets/CleanupItems/Logs.ico",
                        "Contains diagnostic event tracing log files (.etl) generated during the Windows Update process.",
                        "WIN_UPDATE_LOGS",
                        winUpdateLogPath,
                        true).ConfigureAwait(false);

                    // --------------------------------------------
                    // Windows.old (Previous Windows Installations)
                    // --------------------------------------------
                    string windowsOldPath = Path.Combine(systemRoot, "Windows.old");

                    CleanTarget[] windowsOldTargets;
                    if (Directory.Exists(windowsOldPath))
                        windowsOldTargets = new[] { new WindowsOldCleanTarget() { Path = windowsOldPath } };
                    else
                        windowsOldTargets = Array.Empty<CleanTarget>();

                    await AddItem(
                        "Previous Windows Installations",
                        windowsOldTargets,
                        "ms-appx:///Assets/CleanupItems/PreviousWindowsInstallations.ico",
                        "Stores system configuration files and data from previous versions of Windows. Wiping this prevents rollback capabilities but reclaims massive amounts of storage.",
                        "WINDOWS_OLD",
                        windowsOldPath,
                        false).ConfigureAwait(false);

                    // ------------------------------
                    // Memory dump files (MEMORY.DMP)
                    // ------------------------------
                    string mainDumpFile = Path.Combine(windowsRoot, "MEMORY.DMP");
                    var mainDumpTargets = File.Exists(mainDumpFile)
                        ? [ new CleanTarget() { Path = mainDumpFile, Depth = SearchDepth.TopDirectoryOnly } ]
                        : Array.Empty<CleanTarget>();

                    await AddItem(
                        "System Error Memory Dump Files",
                        mainDumpTargets,
                        "ms-appx:///Assets/CleanupItems/SystemLog.ico",
                        "Captures the full contents of system memory during a crash for diagnostic analysis. These files can be massive.",
                        "MEMORY_DUMP",
                        windowsRoot,
                        false).ConfigureAwait(false);

                    // --------------------------------
                    // Minidump files (Minidump folder)
                    // --------------------------------
                    string miniDumpFolder = Path.Combine(windowsRoot, "Minidump");
                    var miniDumpTargets = Directory.Exists(miniDumpFolder)
                        ? [ new CleanTarget() { Path = miniDumpFolder, Depth = SearchDepth.AllDirectories } ]
                        : Array.Empty<CleanTarget>();

                    await AddItem(
                        "System Error Minidump Files",
                        miniDumpTargets,
                        "ms-appx:///Assets/CleanupItems/SystemLog.ico",
                        "Contains smaller individual memory dump files created during historical system crashes.",
                        "MINIDUMP",
                        windowsRoot,
                        false).ConfigureAwait(false);

                    // -------------------
                    // Temp files (System)
                    // -------------------
                    string systemTempPath = Path.Combine(windowsRoot, "Temp");

                    var tempTargets = Directory.Exists(systemTempPath)
                        ? [ new CleanTarget() { Path = systemTempPath, Depth = SearchDepth.AllDirectories } ]
                        : Array.Empty<CleanTarget>();

                    await AddItem(
                        "Temporary Windows Installation Files",
                        tempTargets,
                        "ms-appx:///Assets/CleanupItems/TemporaryFiles.ico",
                        "Contains temporary scratch files used by the operating system, Windows Update, and system services during installation and maintenance tasks.",
                        "WINDOWS_TEMP",
                        systemTempPath,
                        false).ConfigureAwait(false);
                }
            }
        }).ConfigureAwait(true);

        // Back on the UI thread: bulk add to the ObservableCollection to prevent UI stutter
        LoadProperties(discoveredItems);
    }


    public static void CleanUnusedTemp(string tempFolderPath)
    {
        if (!Directory.Exists(tempFolderPath)) return;

        var directory = new DirectoryInfo(tempFolderPath);

        // 1. Get the exact time the computer started.
        // Files older than the current Windows session are 100% safe to delete, 
        // because the app that made them isn't running anymore.
        DateTime bootTime = DateTime.Now - TimeSpan.FromMilliseconds(Environment.TickCount64);

        // 2. Set a short buffer for the current session (e.g., 20 minutes)
        // This catches that 12 GB installer dump from 30 minutes ago, 
        // but protects a file an app created 45 seconds ago.
        DateTime shortSessionBuffer = DateTime.Now.AddMinutes(-20);

        foreach (FileInfo file in directory.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            try
            {
                // Deletion Conditions:
                // - File was made in a previous Windows session OR
                // - File is older than our 20-minute safety buffer
                if (file.LastWriteTime < bootTime || file.LastWriteTime < shortSessionBuffer)
                {
                    file.Attributes = FileAttributes.Normal;
                    file.Delete();
                }
            }
            catch (IOException)
            {
                // File is actively locked by a running process (Truly "In Use"). 
                // Windows handles this for us; skip it safely.
            }
            catch (UnauthorizedAccessException)
            {
                // Lacks permissions, skip safely.
            }
        }
    }

    private async void LoadProperties(List<CleanItem> discoveredItems)
    {
        foreach (var item in discoveredItems)
        {
            CleanItems.Add(item);

            _ = Task.Run(async () =>
            {
                long totalSize = 0;
                int totalCount = 0;

                foreach (var target in item.CleanTargets)
                {
                    var (size, count) = await target.EnumerateAsync(null, CancellationToken.None).ConfigureAwait(false);
                    totalSize += size;
                    totalCount += count;
                }

                var task = App.MainWindow?.DispatcherQueue.EnqueueAsync(() =>
                {
                    item.Size = totalSize;
                    item.FileCount = totalCount;

                    FilesSize += totalSize;
                    FilesCount += totalCount;
                });
                await task!.ConfigureAwait(true);
            });
        }

    }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        CanItemsBeClicked = false;

        foreach (CleanItem item in CleanItems)
        {
            try
            {
                // Check if the path matches and if the item is checked
                if (item.IsChecked)
                {
                    foreach (var target in item.CleanTargets)
                    {
                        await target.ExecuteCleanAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                }
            }
            catch
            {

            }
        }

        await RefreshCleanupListAsync(DriveItems[SelectedDriveIndex]).ConfigureAwait(false);

        CanItemsBeClicked = true;
    }

    [RelayCommand]
    public async Task ViewFilesAsync(int index)
    {
        if (index != -1)
            await Launcher.LaunchFolderPathAsync(CleanItems[index].LaunchTargetPath);
    }

    [RelayCommand]
    public static void RelaunchAsAdmin()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Environment.ProcessPath,
            UseShellExecute = true,
            Verb = "runas"
        });
        Process.GetCurrentProcess().Kill();
    }
}