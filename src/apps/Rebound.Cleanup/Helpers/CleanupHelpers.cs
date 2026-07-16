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
using System.Threading.Tasks;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.TOKEN;
using static TerraFX.Interop.Windows.SE;
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
            "ms-appx:///Assets/Chrome.png", 
            ["chrome"],
            [Path.Combine(_localAppData, @"Google\Chrome\User Data")],
            [ "Google.Chrome" ], 
            @"LocalCache\Local\Google\Chrome\User Data",
            true),

        new(
            "Microsoft Edge", 
            "EDGE", 
            "ms-appx:///Assets/Edge.png",
            ["msedge"],
            [Path.Combine(_localAppData, @"Microsoft\Edge\User Data")], 
            [ "Microsoft.MicrosoftEdge" ], 
            @"LocalCache\Local\Microsoft\Edge\User Data",
            true),

        new(
            "Brave Browser", 
            "BRAVE", 
            "ms-appx:///Assets/Brave.png",
            ["brave"],
            [Path.Combine(_localAppData, @"BraveSoftware\Brave-Browser\User Data")],
            [ "BraveSoftware.BraveBrowser" ], 
            @"LocalCache\Local\BraveSoftware\Brave-Browser\User Data",
            true),

        new(
            "Opera", 
            "OPERA", 
            "ms-appx:///Assets/Opera.png", 
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
            "ms-appx:///Assets/Firefox.png",
            ["firefox"],
            [Path.Combine(_appData, @"Mozilla\Firefox\Profiles")],
            [ "Mozilla.Firefox" ],
            @"LocalCache\Roaming\Mozilla\Firefox\Profiles",
            false)
    ];

    internal async Task<List<CleanItem>> EnumerateBrowsersAsync()
    {
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

                // Firefox
                else
                {
                    foreach (var path in Directory.GetDirectories(root))
                    {
                        // Cache
                        string cPath = Path.Combine(path, "cache2");
                        if (Directory.Exists(cPath)) cacheTargets.Add(new CleanTarget { Path = cPath, Depth = SearchDepth.AllDirectories });

                        // SQLite Engines
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
                    ItemID = $"{browser.Prefix}_CACHE"
                });

            if (cookieTargets.Count > 0)
                itemsList.Add(new(false)
                {
                    Name = $"{browser.Name} - Cookies",
                    Description = $"Clears website cookies and site preferences for {browser.Name}.",
                    CleanTargets = [.. cookieTargets],
                    ItemID = $"{browser.Prefix}_COOKIES"
                });

            if (historyTargets.Count > 0)
                itemsList.Add(new(false)
                {
                    Name = $"{browser.Name} - History{statusSuffix}",
                    Description = $"Deletes the log of visited websites for {browser.Name}.",
                    CleanTargets = [.. historyTargets],
                    ItemID = $"{browser.Prefix}_HISTORY"
                });

            return itemsList;
        }
        return [];
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
        => Depth = SearchDepth.AllDirectories;

    public static unsafe bool EnablePrivilege(string privilegeName)
    {
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

            if (hProcess != HANDLE.NULL)
                CloseHandle(hProcess);
        }
    }

    public void ExecuteDangerousClean()
    {
        if (!Directory.Exists(Path)) return;

        // Elevate process token privileges to allow overriding TrustedInstaller ownership
        // Windows is stubborn and protects these files in case you want to revert after
        // a very "successful" update
        EnablePrivilege("SeTakeOwnershipPrivilege");
        EnablePrivilege("SeRestorePrivilege");

        var rootDir = new DirectoryInfo(Path);
        DeleteDirectoryRecursively(rootDir);
    }

    private static void DeleteDirectoryRecursively(DirectoryInfo dir)
    {
        if ((dir.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
        {
            try { dir.Delete(); } catch { }
            return;
        }

        // Process files first
        foreach (var file in dir.EnumerateFiles())
        {
            try
            {
                StripSecurityAndAttributes(file.FullName);
                file.Delete();
            }
            catch (Exception)
            {
                // Some locked driver files (.sys) require a reboot to clear
                // Technically cleanmgr has private methods to get rid of these
                // but it requires reverse engineering
            }
        }

        // Process subdirectories
        foreach (var subDir in dir.EnumerateDirectories())
            DeleteDirectoryRecursively(subDir);

        // Wipe out the empty directory container
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
            if (string.IsNullOrEmpty(path))
            {
                using ManagedPtr<char> pathPtr = path;

                // Force strip attributes like ReadOnly, System, Hidden
                SetFileAttributesW(pathPtr, FILE.FILE_ATTRIBUTE_NORMAL);

                var adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

                if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    FileSecurity fileSecurity = fileInfo.GetAccessControl();

                    // Overwrite the owner (rip it from TrustedInstaller)
                    fileSecurity.SetOwner(adminSid);
                    fileInfo.SetAccessControl(fileSecurity);

                    // Grant Full Control explicitly
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

                    // Overwrite the owner
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
            // Fail silently on stubborn individual files to keep the cleanup loop rolling
        }
    }
}