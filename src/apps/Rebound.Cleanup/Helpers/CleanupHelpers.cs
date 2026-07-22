// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Cleanup.Items;
using Rebound.Core.Native.Wrappers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.SE;
using static TerraFX.Interop.Windows.TOKEN;
using static TerraFX.Interop.Windows.Windows;

#pragma warning disable CA1031 // Do not catch general exception types

namespace Rebound.Cleanup.Helpers;

internal record BrowserCleanupDefinition(
    string Name,
    string Prefix,
    string IconPath,
    string[] ProcessNames,
    string[] Win32Paths,
    string[] MsixKeywords,
    string MsixSubPath,
    bool IsChromium);

internal class BrowserScannerEngine
{
    private readonly string _localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private readonly string _appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    private List<BrowserCleanupDefinition> GetDefinitions() =>
    [
        new(
            "Google Chrome", 
            "CHROME", 
            "ms-appx:///Assets/CleanupItems/Browsers/Chrome.png", 
            ["chrome"],
            [Path.Combine(_localAppData, @"Google\Chrome\User Data")],
            [ "Google.Chrome" ], 
            @"LocalCache\Local\Google\Chrome\User Data",
            true),

        new(
            "Microsoft Edge", 
            "EDGE", 
            "ms-appx:///Assets/CleanupItems/Browsers/Edge.png",
            ["msedge"],
            [Path.Combine(_localAppData, @"Microsoft\Edge\User Data")], 
            [ "Microsoft.MicrosoftEdge" ], 
            @"LocalCache\Local\Microsoft\Edge\User Data",
            true),

        new(
            "Brave Browser", 
            "BRAVE", 
            "ms-appx:///Assets/CleanupItems/Browsers/Brave.png",
            ["brave"],
            [Path.Combine(_localAppData, @"BraveSoftware\Brave-Browser\User Data")],
            [ "BraveSoftware.BraveBrowser" ], 
            @"LocalCache\Local\BraveSoftware\Brave-Browser\User Data",
            true),

        new(
            "Opera", 
            "OPERA", 
            "ms-appx:///Assets/CleanupItems/Browsers/Opera.png", 
            ["opera"],
            [Path.Combine(_appData, @"Opera Software\Opera Stable"),
            Path.Combine(_localAppData, @"Opera Software\Opera Stable")], 
            [ "OperaSoftware.Opera" ],
            @"LocalCache\Local\Opera Software\Opera Stable",
            true),

        // The only non-Chromium browser out there
        new(
            "Mozilla Firefox",
            "FIREFOX",
            "ms-appx:///Assets/CleanupItems/Browsers/Firefox.png",
            ["firefox"],
            [Path.Combine(_appData, @"Mozilla\Firefox\Profiles")],
            [ "Mozilla.Firefox" ],
            @"LocalCache\Roaming\Mozilla\Firefox\Profiles",
            false)
    ];

    internal async Task<List<CleanItem>> EnumerateBrowsersAsync()
    {
        var allItems = new List<CleanItem>();
        foreach (var browser in GetDefinitions())
        {
            // Gather all unique base root paths for this browser (Win32 + MSIX Store packages)
            var roots = GetResolvedRoots(browser);
            if (roots.Count == 0) continue;

            // Check if browser is running
            bool isRunning = browser.ProcessNames.Any(p => Process.GetProcessesByName(p).Length > 0);

            var cacheTargets = new List<CleanTarget>();
            var cookieTargets = new List<CleanTarget>();
            var historyTargets = new List<CleanTarget>();

            // Scan profiles across all detected deployment roots
            foreach (var root in roots)
            {
                if (!Directory.Exists(root)) continue;

                // Almost every browser known to man
                if (browser.IsChromium)
                {
                    // Opera treats the root directly as the profile, others use "Default/Profile X" structures
                    bool directRootProfile = browser.Prefix == "OPERA";
                    var profileDirs = directRootProfile ? [root] : Directory.GetDirectories(root);

                    foreach (var path in profileDirs)
                    {
                        string name = Path.GetFileName(path);
                        if (!directRootProfile && !name.Equals("Default", StringComparison.OrdinalIgnoreCase) && !name.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Cache
                        string cPath = Path.Combine(path, "Cache");
                        if (Directory.Exists(cPath)) cacheTargets.Add(new CleanTarget { Path = cPath, Depth = SearchDepth.AllDirectories });

                        // Cookies & History (Only build targets if the process isn't holding SQLite locks)
                        if (!isRunning)
                        {
                            string hist = Path.Combine(path, "History");
                            if (File.Exists(hist)) historyTargets.Add(new CleanTarget { Path = hist, Depth = SearchDepth.TopDirectoryOnly });

                            string cook = Path.Combine(path, "Network", "Cookies");
                            if (!File.Exists(cook)) cook = Path.Combine(path, "Cookies"); // Fallback
                            if (File.Exists(cook)) cookieTargets.Add(new CleanTarget { Path = cook, Depth = SearchDepth.TopDirectoryOnly });
                        }
                    }
                }

                // Firefox specific scanning logic
                else
                {
                    // Firefox profile scanning
                    foreach (var path in Directory.GetDirectories(root))
                    {
                        string profileFolderName = Path.GetFileName(path);

                        // Route to the correct AppData\Local folder structure depending on Win32 or MSIX deployment
                        string cacheProfilePath = root.Contains("Packages")
                            ? root.Replace(@"LocalCache\Roaming", @"LocalCache\Local")
                            : Path.Combine(_localAppData, @"Mozilla\Firefox\Profiles");

                        string actualCachePath = Path.Combine(cacheProfilePath, profileFolderName, "cache2");

                        if (Directory.Exists(actualCachePath))
                            cacheTargets.Add(new CleanTarget { Path = actualCachePath, Depth = SearchDepth.AllDirectories });

                        // SQLite Databases (History & Cookies reside exclusively in the Roaming root)
                        if (!isRunning)
                        {
                            string hist = Path.Combine(path, "places.sqlite");
                            if (File.Exists(hist)) historyTargets.Add(new CleanTarget { Path = hist, Depth = SearchDepth.TopDirectoryOnly });

                            string cook = Path.Combine(path, "cookies.sqlite");
                            if (File.Exists(cook)) cookieTargets.Add(new CleanTarget { Path = cook, Depth = SearchDepth.TopDirectoryOnly });
                        }
                    }
                }
            }

            // Register perfectly flattened items into the UI pool
            string statusSuffix = isRunning ? " (Close browser to clear databases)" : "";

            var itemsList = new List<CleanItem>();

            if (cacheTargets.Count > 0)
                itemsList.Add(new(true)
                {
                    Name = $"{browser.Name} - Cache",
                    Description = $"Removes cached images and web elements stored by {browser.Name}.",
                    CleanTargets = [.. cacheTargets],
                    ItemID = $"{browser.Prefix}_CACHE",
                    ImagePath = browser.IconPath,
                    LaunchTargetPath = cacheTargets[0].Path,
                });

            if (cookieTargets.Count > 0)
                itemsList.Add(new(false)
                {
                    Name = $"{browser.Name} - Cookies",
                    Description = $"Clears website cookies and site preferences for {browser.Name}.",
                    CleanTargets = [.. cookieTargets],
                    ItemID = $"{browser.Prefix}_COOKIES",
                    ImagePath = browser.IconPath,
                    LaunchTargetPath = cookieTargets[0].Path,
                });

            if (historyTargets.Count > 0)
                itemsList.Add(new(false)
                {
                    Name = $"{browser.Name} - History{statusSuffix}",
                    Description = $"Deletes the log of visited websites for {browser.Name}.",
                    CleanTargets = [.. historyTargets],
                    ItemID = $"{browser.Prefix}_HISTORY",
                    ImagePath = browser.IconPath,
                    LaunchTargetPath = historyTargets[0].Path,
                });

            allItems.AddRange(itemsList);
        }
        return allItems;
    }

    private List<string> GetResolvedRoots(BrowserCleanupDefinition browser)
    {
        var paths = new List<string>(browser.Win32Paths);
        string packages = Path.Combine(_localAppData, "Packages");

        if (Directory.Exists(packages))
        {
            foreach (var kw in browser.MsixKeywords)
            {
                try
                {
                    foreach (var match in Directory.GetDirectories(packages, $"*{kw}*"))
                    {
                        paths.Add(Path.Combine(match, browser.MsixSubPath));
                    }
                }
                catch { /* Quiet skip unreadable ACL'd system profiles */ }
            }
        }
        return [.. paths.Distinct()];
    }
}

internal class WindowsOldCleanTarget : CleanTarget
{
    public WindowsOldCleanTarget()
    {
        Depth = SearchDepth.AllDirectories;
        DeleteEmptySubdirectories = true;

        // Route the orchestrator directly into your fixed engine architecture
        CustomDeleteAction = async (target, progressReporter, cancellationToken) =>
        {
            await Task.Run(() =>
            {
                if (!Directory.Exists(target.Path)) return;

                // 1. Elevate process token tokens to bypass TrustedInstaller locks
                EnablePrivilege("SeTakeOwnershipPrivilege");
                EnablePrivilege("SeRestorePrivilege");
                EnablePrivilege("SeSecurityPrivilege"); // Crucial for security descriptor modification

                var rootDir = new DirectoryInfo(target.Path);

                // 2. Execute the recursive cleanup
                long batchSize = 0;
                int batchCount = 0;
                string lastReportedPath = string.Empty;
                var stopwatch = Stopwatch.StartNew();

                DeleteDirectoryRecursively(rootDir, progressReporter, ref batchSize, ref batchCount, ref lastReportedPath, stopwatch, cancellationToken);

                // 3. Flush the final metric totals back to the UI thread
                if (batchCount > 0)
                {
                    progressReporter?.Report((batchSize, batchCount, lastReportedPath));
                }
            }, cancellationToken).ConfigureAwait(false);
        };
    }

    private static void DeleteDirectoryRecursively(
        DirectoryInfo dir,
        IProgress<(long size, int count, string itemPath)> progressReporter,
        ref long batchSize,
        ref int batchCount,
        ref string lastReportedPath,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Safe skip check for symlink junctions to avoid bouncing into active system folders
        if ((dir.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
        {
            try { dir.Delete(); } catch { }
            return;
        }

        // Process files first
        foreach (var file in dir.EnumerateFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                long length = file.Length;
                string fullName = file.FullName;

                StripSecurityAndAttributes(fullName);
                file.Delete();

                batchSize += length;
                batchCount++;
                lastReportedPath = fullName;

                if (stopwatch.ElapsedMilliseconds > 100)
                {
                    progressReporter?.Report((batchSize, batchCount, lastReportedPath));
                    batchSize = 0;
                    batchCount = 0;
                    stopwatch.Restart();
                }
            }
            catch { /* Skip stubborn active driver files safely */ }
        }

        // Process subdirectories recursively
        foreach (var subDir in dir.EnumerateDirectories())
        {
            DeleteDirectoryRecursively(subDir, progressReporter, ref batchSize, ref batchCount, ref lastReportedPath, stopwatch, cancellationToken);
        }

        // Wipe out the empty directory container container itself
        try
        {
            StripSecurityAndAttributes(dir.FullName);
            dir.Delete();
        }
        catch { }
    }

    private static unsafe void StripSecurityAndAttributes(string path)
    {
        try
        {
            // FIXED: Inverted string guard error corrected
            if (!string.IsNullOrEmpty(path))
            {
                using ManagedPtr<char> pathPtr = path;

                // Force strip system, hidden, and read-only flags
                SetFileAttributesW(pathPtr, FILE.FILE_ATTRIBUTE_NORMAL);

                var adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

                if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    FileSecurity fileSecurity = fileInfo.GetAccessControl();

                    fileSecurity.SetOwner(adminSid);
                    fileInfo.SetAccessControl(fileSecurity);

                    fileSecurity.AddAccessRule(new FileSystemAccessRule(
                        adminSid,
                        FileSystemRights.FullControl,
                        AccessControlType.Allow));
                    fileInfo.SetAccessControl(fileSecurity);
                }
                else if (Directory.Exists(path))
                {
                    var dirInfo = new DirectoryInfo(path);
                    DirectorySecurity dirSecurity = dirInfo.GetAccessControl();

                    dirSecurity.SetOwner(adminSid);
                    dirInfo.SetAccessControl(dirSecurity);

                    dirSecurity.AddAccessRule(new FileSystemAccessRule(
                        adminSid,
                        FileSystemRights.FullControl,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Allow));
                    dirInfo.SetAccessControl(dirSecurity);
                }
            }
        }
        catch
        {
            // Fall through safely to keep loops moving
        }
    }

    public static unsafe bool EnablePrivilege(string privilegeName)
    {
        // Obtain current process token wrapper
        var hProcess = new HANDLE((void*)Process.GetCurrentProcess().Handle);
        var hToken = new HANDLE();

        if (!OpenProcessToken(hProcess, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, &hToken))
            return false;

        try
        {
            using ManagedPtr<char> privilegeNamePtr = privilegeName;
            using ManagedPtr<LUID> luid = default;

            if (!LookupPrivilegeValueW(null!, privilegeNamePtr, luid))
                return false;

            TOKEN_PRIVILEGES tp = default;
            tp.PrivilegeCount = 1;
            tp.Privileges.e0.Luid = luid;
            tp.Privileges.e0.Attributes = SE_PRIVILEGE_ENABLED;

            return AdjustTokenPrivileges(hToken, false, &tp, (uint)sizeof(TOKEN_PRIVILEGES), null, null);
        }
        finally
        {
            if (hToken != HANDLE.NULL)
                CloseHandle(hToken);

            // FIXED: Do NOT close the process pseudo-handle context!
        }
    }
}