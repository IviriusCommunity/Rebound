// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
using Windows.Foundation;
using Windows.System;
using WinUIEx;
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

    [ObservableProperty] public partial int MaxProgress { get; set; }

    [ObservableProperty] public partial int CurrentProgress { get; set; }

    [ObservableProperty] public partial string CurrentTask { get; set; }

    [ObservableProperty] public partial string CurrentOperation { get; set; }

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

    private CancellationTokenSource _globalIndexingCancellationToken = new();

    [RelayCommand]
    public async Task RefreshCleanupListAsync(DriveComboBoxItem selectedItem)
    {
        if (selectedItem == null)
            return;

        // Cancel ongoing indexing operations
        await _globalIndexingCancellationToken.CancelAsync().ConfigureAwait(true);
        _globalIndexingCancellationToken = new CancellationTokenSource();

        // Reset the list and properties to default
        CleanItems.Clear();
        FilesSize = 0;
        FilesCount = 0;

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
                        CustomDeleteAction = async (target, progressReporter, cancellationToken) =>
                        {
                            await Task.Run(() =>
                            {
                                var recycleBinRootPath = Path.Combine(selectedItem.DrivePath, "$Recycle.Bin");
                                if (!Directory.Exists(recycleBinRootPath))
                                    return;

                                var rootDirectory = new DirectoryInfo(recycleBinRootPath);

                                // Safe enumeration options to avoid system loops/junction blocks
                                var options = new EnumerationOptions
                                {
                                    IgnoreInaccessible = true,
                                    RecurseSubdirectories = true,
                                    AttributesToSkip = FileAttributes.ReparsePoint // Prevents escaping the Recycle Bin via junctions
                                };

                                long batchSize = 0;
                                int batchCount = 0;
                                string lastReportedFilePath = string.Empty;
                                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                                // Loop through all SID folders (e.g., S-1-5-21-...) belonging to different users
                                foreach (var subDirectory in rootDirectory.EnumerateDirectories())
                                {
                                    try
                                    {
                                        // Strip hidden/system flags so Windows allows the deletion loop
                                        subDirectory.Attributes = FileAttributes.Normal;

                                        // Clear out all deleted file fragments and metadata indexes inside safely
                                        foreach (FileInfo file in subDirectory.EnumerateFiles("*", options))
                                        {
                                            cancellationToken.ThrowIfCancellationRequested();

                                            try
                                            {
                                                long fileSize = file.Length;

                                                file.Attributes = FileAttributes.Normal;
                                                file.Delete();

                                                // Only accumulate progress metrics on successful execution
                                                batchSize += fileSize;
                                                batchCount++;
                                                lastReportedFilePath = file.FullName;

                                                if (stopwatch.ElapsedMilliseconds > 100)
                                                {
                                                    progressReporter?.Report((batchSize, batchCount, lastReportedFilePath));

                                                    batchSize = 0;
                                                    batchCount = 0;
                                                    stopwatch.Restart();
                                                }
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

                                // Flush any remaining completed deletions left in the final batch window
                                if (batchCount > 0)
                                {
                                    progressReporter?.Report((batchSize, batchCount, lastReportedFilePath));
                                }
                            }, cancellationToken).ConfigureAwait(false);
                        },

                        // Custom enumerate action for Admin mode
                        CustomEnumerateAction = async (target, progressReporter, cancellationToken) =>
                        {
                            return await Task.Run(() =>
                            {
                                var recycleBinRootPath = Path.Combine(selectedItem.DrivePath, "$Recycle.Bin");
                                if (!Directory.Exists(recycleBinRootPath)) return (0L, 0);

                                long totalSizeBytes = 0;
                                int totalFiles = 0;

                                // Trackers for our throttled batching
                                long batchSize = 0;
                                int batchCount = 0;
                                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                                var rootDirectory = new DirectoryInfo(recycleBinRootPath);

                                foreach (var subDirectory in rootDirectory.EnumerateDirectories())
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    try
                                    {
                                        foreach (var file in subDirectory.EnumerateFiles("*", SearchOption.AllDirectories))
                                        {
                                            cancellationToken.ThrowIfCancellationRequested(); // Check inside the inner file loops too
                                            try
                                            {
                                                totalSizeBytes += file.Length;
                                                totalFiles++;

                                                // Accumulate current batch
                                                batchSize += file.Length;
                                                batchCount++;

                                                // Push updates to UI every 100ms
                                                if (stopwatch.ElapsedMilliseconds > 100)
                                                {
                                                    progressReporter?.Report((batchSize, batchCount, file.FullName));

                                                    batchSize = 0;
                                                    batchCount = 0;
                                                    stopwatch.Restart();
                                                }
                                            }
                                            catch (FileNotFoundException) { /* Gone during scan */ }
                                            catch (UnauthorizedAccessException) { /* Hidden system file lock */ }
                                        }
                                    }
                                    catch (Exception) { /* Skip unreadable user SID directories */ }
                                }

                                // Flush any remaining items in the batch at the finish line
                                if (batchCount > 0)
                                {
                                    progressReporter?.Report((batchSize, batchCount, rootDirectory.FullName));
                                }

                                return (totalSizeBytes, totalFiles);
                            }, cancellationToken).ConfigureAwait(false);
                        }
                    }],
                    "ms-appx:///Assets/CleanupItems/RecycleBin.ico",
                    "The Recycle Bins for all user accounts on this computer. Deleted files and folders stored here can normally be restored until they are permanently removed. Emptying the Recycle Bin permanently deletes these items and frees the disk space they occupy.",
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
                        CustomDeleteAction = async (target, progressReporter, cancellationToken) =>
                        {
                            await Task.Run(() =>
                            {
                                unsafe
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    uint flags = SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND;
        
                                    // drivePath must be formatted like "C:\"
                                    using ManagedPtr<char> drivePathPtr = selectedItem.DrivePath;

                                    var rbInfo = new SHQUERYRBINFO { cbSize = (uint)sizeof(SHQUERYRBINFO) };
                                    int queryResult = SHQueryRecycleBinW(drivePathPtr, &rbInfo);

                                    long totalSizeToFree = rbInfo.i64Size;
                                    int totalCountToFree = (int)rbInfo.i64NumItems;

                                    // This clears the current user's partition on that specific drive
                                    int result = SHEmptyRecycleBinW(HWND.NULL, drivePathPtr, flags); 
            
                                    // If the native call succeeds (S_OK is 0), report completion to the UI
                                    if (result == 0 && totalCountToFree > 0)
                                    {
                                        progressReporter?.Report((totalSizeToFree, totalCountToFree, selectedItem.DrivePath));
                                    }
                                }
                            }, cancellationToken).ConfigureAwait(false);
                        },

                        // Custom enumerate action for Non-Admin mode
                        CustomEnumerateAction = async (target, progressReporter, cancellationToken) =>
                        {
                            return await Task.Run(() =>
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                unsafe
                                {
                                    // Setup the info structure sizes
                                    var rbInfo = new SHQUERYRBINFO { cbSize = (uint)sizeof(SHQUERYRBINFO) };

                                    using ManagedPtr<char> drivePathPtr = selectedItem.DrivePath;

                                    // Native query invoke
                                    int result = SHQueryRecycleBinW(drivePathPtr, &rbInfo);
                                    if (result == S_OK)
                                    {
                                        // We got the data instantly! Report it as a single batch to update the UI
                                        progressReporter?.Report((rbInfo.i64Size, (int)rbInfo.i64NumItems, selectedItem.DrivePath));

                                        return (rbInfo.i64Size, (int)rbInfo.i64NumItems);
                                    }
                                }

                                return (0L, 0);
                            }, cancellationToken).ConfigureAwait(false);
                        }
                    }],
                    "ms-appx:///Assets/CleanupItems/RecycleBin.ico",
                    "The Recycle Bin for the current user account. Deleted files and folders stored here can normally be restored until they are permanently removed. Emptying the Recycle Bin permanently deletes these items and frees the disk space they occupy.",
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
                    [new() { Path = tempPath, Depth = SearchDepth.AllDirectories, DeleteEmptySubdirectories = true }],
                    "ms-appx:///Assets/CleanupItems/TemporaryFiles.ico",
                    "Temporary files created by applications and Windows in the current user's temporary folder. These files are intended for short-term use and are generally safe to delete after the applications that created them have closed.",
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
                    [new() { Path = recentFilesPath, Depth = SearchDepth.AllDirectories }],
                    "ms-appx:///Assets/CleanupItems/RecentFiles.ico",
                    "Shortcuts to recently opened files and documents used by File Explorer and applications to display recent activity. Clearing this list removes the shortcuts without affecting the original files.",
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
                    [new() { Path = inetCachePath, Depth = SearchDepth.AllDirectories, DeleteEmptySubdirectories = true }],
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
                        CustomDeleteAction = async (target, progressReporter, cancellationToken) =>
                        {
                            await Task.Run(() =>
                            {
                                if (!Directory.Exists(target.Path)) return;

                                var dirInfo = new DirectoryInfo(target.Path);

                                var options = new EnumerationOptions
                                {
                                    IgnoreInaccessible = true,
                                    RecurseSubdirectories = target.Depth == SearchDepth.AllDirectories,
                                    AttributesToSkip = FileAttributes.ReparsePoint // Keep it safe from symlink traps
                                };

                                long batchSize = 0;
                                int batchCount = 0;
                                string lastReportedFilePath = string.Empty;
                                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                                foreach (FileInfo file in dirInfo.EnumerateFiles("*", options))
                                {
                                    cancellationToken.ThrowIfCancellationRequested();

                                    // Apply the filter so it doesn't touch other Explorer files
                                    if (target.FileFilter(file))
                                    {
                                        try
                                        {
                                            long fileSize = file.Length;

                                            // Strip readonly/system attributes just in case
                                            if (file.Attributes != FileAttributes.Normal)
                                            {
                                                file.Attributes = FileAttributes.Normal;
                                            }

                                            file.Delete();

                                            // Only track progress metrics if the deletion was successful
                                            batchSize += fileSize;
                                            batchCount++;
                                            lastReportedFilePath = file.FullName;

                                            if (stopwatch.ElapsedMilliseconds > 100)
                                            {
                                                progressReporter?.Report((batchSize, batchCount, lastReportedFilePath));

                                                batchSize = 0;
                                                batchCount = 0;
                                                stopwatch.Restart();
                                            }
                                        }
                                        catch (IOException)
                                        {
                                            // File is locked by Explorer for reasons™️
                                        }
                                        catch (UnauthorizedAccessException) { /* Insufficient permissions */ }
                                    }
                                }

                                // Flush any remaining completed deletions left in the final batch window
                                if (batchCount > 0)
                                {
                                    progressReporter?.Report((batchSize, batchCount, lastReportedFilePath));
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
                    else
                    {
                        // Emergency fallback if the Win32 API call fails
                        downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    }
                }

                await AddItem(
                    "Downloads Folder (Current User)",
                    [new() { Path = downloadsPath, Depth = SearchDepth.AllDirectories, DeleteEmptySubdirectories = true }],
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
                        ? [ new CleanTarget() { Path = dpfPath, Depth = SearchDepth.TopDirectoryOnly, DeleteEmptySubdirectories = true } ]
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
                        ? [ new CleanTarget() { Path = wdoCachePath, Depth = SearchDepth.AllDirectories, DeleteEmptySubdirectories = true } ]
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
                        ? [ new CleanTarget() { Path = updateStagingPath, Depth = SearchDepth.AllDirectories, DeleteEmptySubdirectories = true } ]
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
                        ? [ new CleanTarget() { Path = systemTempPath, Depth = SearchDepth.AllDirectories, DeleteEmptySubdirectories = true } ]
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
        await LoadPropertiesAsync(discoveredItems, _globalIndexingCancellationToken.Token).ConfigureAwait(false);
    }

    private async Task LoadPropertiesAsync(List<CleanItem> discoveredItems, CancellationToken token)
    {
        MaxProgress = discoveredItems.Count;
        CurrentOperation = "Indexing items...";

        var indexingTasks = new List<Task>();

        foreach (var item in discoveredItems)
        {
            if (token.IsCancellationRequested)
                break;

            CleanItems.Add(item);

            // Define the progress handler ON the UI thread.
            // IProgress automatically handles the Dispatcher switching for us.
            var progress = new Progress<(long size, int count, string itemPath)>(async delta =>
            {
                // Safeguard: If the user cancelled, stop adding to global aggregates
                if (token.IsCancellationRequested)
                    return;

                // Display the currently indexed item in the UI
                await (App.MainWindow?.DispatcherQueue.EnqueueAsync(() =>
                {
                    CurrentTask = delta.itemPath;
                }))!.ConfigureAwait(false)!;

                // Update the individual row item in real-time
                item.Size += delta.size;
                item.FileCount += delta.count;

                // Accumulate into your global totals in real-time
                FilesSize += delta.size;
                FilesCount += delta.count;
            });

            var task = Task.Run(async () =>
            {
                foreach (var target in item.CleanTargets)
                {
                    // Pass our UI-bound progress reporter down to the scanner
                    var (finalSize, finalCount) = await target.EnumerateAsync(progress, token).ConfigureAwait(false);
                }

                if (token.IsCancellationRequested)
                    return;

                // Final flip of the flag once this specific item finishes entirely
                await (App.MainWindow?.DispatcherQueue.EnqueueAsync(async () =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        item.IsEnumerated = true;
                        await (App.MainWindow?.DispatcherQueue.EnqueueAsync(() =>
                        {
                            CurrentProgress++;
                        }))!.ConfigureAwait(false)!;
                    }
                }))!.ConfigureAwait(false)!;
            }, token);

            indexingTasks.Add(task);
        }

        try
        {
            await Task.WhenAll(indexingTasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Suppress expected cancellation exceptions gracefully
        }

        await (App.MainWindow?.DispatcherQueue.EnqueueAsync(() =>
        {
            CurrentProgress = 0;
            CurrentTask = "Done.";
            CurrentOperation = "Idle";
        }))!.ConfigureAwait(false);
    }

    [RelayCommand]
    public async Task DeleteAsync()
    {
        // 1. Gather up all targets that the user actually selected to clean
        var activeTargets = CleanItems
            .Where(item => item.IsChecked)
            .SelectMany(item => item.CleanTargets)
            .ToList();

        var itemTargets = CleanItems
            .Where(item => item.IsChecked)
            .ToList();

        if (activeTargets.Count == 0) return;

        // Calculate total operations to process (matches our file counts from enumeration)
        // Assuming target exposes the Count calculated during EnumerateAsync
        int totalFilesToClean = itemTargets.Sum(t => t.FileCount);
        long totalBytesToClean = itemTargets.Sum(t => t.Size);

        // 2. Set up Cancellation and Window Closing Guards
        using var cts = new CancellationTokenSource();

        TypedEventHandler<AppWindow, AppWindowClosingEventArgs> preventCloseHandler = (s, e) => e.Cancel = true;
        if (App.MainWindow != null)
        {
            App.MainWindow.AppWindow.Closing += preventCloseHandler;
        }

        // 3. Build out the live progress UI layout completely in code
        var statusText = new TextBlock
        {
            Text = "Preparing extraction...",
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = totalFilesToClean,
            Value = 0,
            IsIndeterminate = totalFilesToClean == 0 // Failsafe if counts were zero
        };

        var contentPanel = new StackPanel { Spacing = 8 };
        contentPanel.Children.Add(statusText);
        contentPanel.Children.Add(progressBar);

        var progressDialog = new ContentDialog
        {
            Title = "Cleaning...",
            Content = contentPanel,
            SecondaryButtonText = "Cancel",
            XamlRoot = App.MainWindow?.Content?.XamlRoot // Required for WinUI 3 dialogs
        };

        progressDialog.SecondaryButtonClick += (s, e) =>
        {
            cts.Cancel();
        };

        // 4. Setup Progress Aggregator to report back safely to the UI thread
        long aggregatedBytesFreed = 0;
        int aggregatedFilesFreed = 0;

        var progressReporter = new Progress<(long size, int count, string itemPath)>(data =>
        {
            aggregatedBytesFreed += data.size;
            aggregatedFilesFreed += data.count;

            // Keep progress bar bounded in case file discrepancies happen mid-run
            progressBar.Value = Math.Min(aggregatedFilesFreed, progressBar.Maximum);
            statusText.Text = data.itemPath;
            ToolTipService.SetToolTip(statusText, data.itemPath);
        });

        // Show the progress dialog without awaiting its full completion block yet
        var dialogTask = progressDialog.ShowAsync().AsTask();

        bool isCancelled = false;

        // 5. Run the core background cleaning engine loops
        try
        {
            foreach (var target in activeTargets)
            {
                if (cts.IsCancellationRequested)
                {
                    isCancelled = true;
                    break;
                }

                try
                {
                    // Execute using our updated signature with progress reporting
                    await target.ExecuteCleanAsync(progressReporter, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    isCancelled = true;
                    break;
                }
                catch
                {
                    // Engine skips individual locked targets cleanly
                }
            }
        }
        finally
        {
            await (App.MainWindow?.DispatcherQueue.EnqueueAsync(async () =>
            {
                // 6. Clean up hooks and dismiss live tracking UI
                if (App.MainWindow != null)
                {
                    App.MainWindow.AppWindow.Closing -= preventCloseHandler;
                }

                progressDialog.Hide();
                await dialogTask.ConfigureAwait(true); // Ensure dialog finishes execution loop gracefully
            }))!.ConfigureAwait(false);
        }

        await (App.MainWindow?.DispatcherQueue.EnqueueAsync(async () =>
        {
            // 7. Refresh the UI elements behind the scenes
            if (DriveItems.Count > SelectedDriveIndex)
            {
                await RefreshCleanupListAsync(DriveItems[SelectedDriveIndex]).ConfigureAwait(true);
            }

            // 8. Present success validation modal to user if they didn't abort execution
            if (!isCancelled)
            {
                var completionDialog = new ContentDialog
                {
                    Title = "All done!",
                    Content = $"Cleaned {aggregatedFilesFreed:N0} files ({FormatBytes(aggregatedBytesFreed)})",
                    PrimaryButtonText = "Ok",
                    XamlRoot = App.MainWindow?.Content?.XamlRoot
                };

                await completionDialog.ShowAsync();
            }
        }))!.ConfigureAwait(false);
    }

    /// <summary>
    /// Converts raw disk capacities into clean human-readable binary byte string formats.
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "bytes", "KB", "MB", "GB", "TB", "PB" };
        int counter = 0;
        double number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:N2} {suffixes[counter]}";
    }

    [RelayCommand]
    public async Task ViewFilesAsync(int index)
    {
        try
        {
            if (index != -1)
                await Launcher.LaunchFolderPathAsync(CleanItems[index].LaunchTargetPath);
        }
        catch
        {
            App.MainWindow?.CreateMessageDialog("The contents of this item cannot be viewed.", "No access");
        }
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