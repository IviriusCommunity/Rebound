using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using IWshRuntimeLibrary;
using Rebound.Defrag.Controls;
using Rebound.Helpers;
using Windows.Win32;

namespace Rebound.Defrag.Helpers;

internal static class DriveHelper
{
    // For system drives
    internal class VolumeInfo
    {
        public string? GUID { get; set; }

        public string? FileSystem { get; set; }

        public ulong Size { get; set; }

        public string? FriendlyName { get; set; }
    }

    internal static List<VolumeInfo> GetSystemVolumes()
    {
        // Initialize collection
        List<VolumeInfo> volumes = [];

        // WMI query
        var query = 
            // Get Win32_Volume
            "SELECT * FROM Win32_Volume " +

            // Where the volume doesn't have an assigned drive letter
            "WHERE DriveLetter IS NULL";

        // Query WMI
        using (ManagementObjectSearcher searcher = new(query))
        {
            foreach (var volume in searcher.Get().Cast<ManagementObject>())
            {
                // Get the \\?\Volume{GUID} path
                var volumePath = volume["DeviceID"].ToString(); 

                // Get the file system (FAT32, NTFS, etc.)
                var fileSystem = volume["FileSystem"]?.ToString() ?? "Unknown";
                var size = (ulong)volume["Capacity"];

                // Query properties
                var friendlyName = fileSystem == "FAT32" && size < 512 * 1024 * 1024
                    // EFI
                    ? "EFI System Partition"
                    : fileSystem == "NTFS" && size > 500 * 1024 * 1024

                        // Recovery
                        ? "Recovery Partition"
                        : fileSystem == "NTFS" && size < 500 * 1024 * 1024

                            // System reserved
                            ? "System Reserved Partition"

                            // Another partition the user might have created
                            : "Unknown Partition";

                // Add the volume information to the collection
                volumes.Add(new VolumeInfo
                {
                    GUID = volumePath,
                    FileSystem = fileSystem,
                    Size = size,
                    FriendlyName = friendlyName
                });
            }
        }

        // Return volumes
        return volumes;
    }

    public static unsafe ObservableCollection<DriveListViewItem> GetDriveItems(bool loadSystemPartitions)
    {
        // The drive items
        ObservableCollection<DriveListViewItem> items = [];

        // Get the logical drives bitmask
        var drivesBitMask = PInvoke.GetLogicalDrives();

        // If there are no drives return
        if (drivesBitMask is 0)
        {
            return [];
        }

        // Obtain each drive
        for (var singularDriveLetter = 'A'; singularDriveLetter <= 'Z'; singularDriveLetter++)
        {
            // Get mask
            var mask = 1u << (singularDriveLetter - 'A');

            if ((drivesBitMask & mask) != 0)
            {
                // Convert single drive letter into drive path of format C:\
                var drive = $"{singularDriveLetter}:\\";

                uint serialNumber,
                    maxComponentLength,
                    fileSystemFlags;

                Span<char> volumeName = new char[261];
                Span<char> fileSystemName = new char[261];

                // Obtain volume information using P/Invoke
                if (PInvoke.GetVolumeInformation(drive, volumeName, &serialNumber, &maxComponentLength, &fileSystemFlags, fileSystemName))
                {
                    // Convert singular drive letter of format C into drive letter of format C:
                    var driveLetter = $"{singularDriveLetter}:";

                    // Obtain the drive media type for drive path
                    var mediaType = GetDriveTypeDescriptionAsync(drive);

                    // Set the icon for the drive
                    var imagePath = mediaType switch
                    {
                        "Removable" => "ms-appx:///Assets/DriveRemovable.png",
                        "Unknown" => "ms-appx:///Assets/DriveUnknown.png",
                        "CD-ROM" => "ms-appx:///Assets/DriveOptical.png",
                        _ => "ms-appx:///Assets/Drive.png"
                    };

                    imagePath = driveLetter == EnvironmentHelper.GetWindowsInstallationDrivePath().DrivePathToLetter() ? "ms-appx:///Assets/DriveWindows.png" : imagePath;

                    // Create the drive item
                    var item = new DriveListViewItem(
                        string.IsNullOrEmpty(volumeName.ToString()) ? $"({driveLetter})" : $"{volumeName} ({driveLetter})",
                        drive,
                        imagePath,
                        mediaType);

                    item.LastOptimized = item.GetLastOptimized();

                    // Add to collection
                    items.Add(item);
                }
            }
        }

        // Obtain system partitions
        if (loadSystemPartitions)
        {
            // Get the system partitions
            var systemPartitions = GetSystemVolumes();

            // Add system partitions to the items list
            foreach (var result in systemPartitions)
            {
                var driveMediaType = string.Empty;

                // Use the same media type the Windows installation drive has
                foreach (var volume in items)
                {
                    driveMediaType = volume.DrivePath == EnvironmentHelper.GetWindowsInstallationDrivePath() ? volume.MediaType : driveMediaType;
                }

                // Create the drive item
                var item = new DriveListViewItem(
                    result.FriendlyName,
                    result.GUID,
                    "ms-appx:///Assets/DriveSystem.png",
                    driveMediaType);

                item.LastOptimized = item.GetLastOptimized();

                // Add to collection
                items.Add(item);
            }
        }

        // Return the drives list
        return items;
    }

    public static string GetDriveTypeDescriptionAsync(string driveRoot)
    {
        var driveType = PInvoke.GetDriveType(driveRoot);

        return driveType switch
        {
            2 => "Removable",        // DRIVE_REMOVABLE = 2
            3 => GetDiskDriveFromLetter(driveRoot),  // DRIVE_FIXED = 3
            4 => "Network",          // DRIVE_REMOTE = 4
            5 => "CD-ROM",           // DRIVE_CDROM = 5
            6 => "RAM Disk",         // DRIVE_RAMDISK = 6
            1 => "No Root Directory", // DRIVE_NO_ROOT_DIR = 1
            _ => "Unknown",
        };
    }

    // Caching
    private static readonly object cacheLock = new();
    private static Dictionary<string, string>? cachedDiskMappings;
    private static List<PhysicalDisk>? cachedPhysicalDisks;
    private static DateTime lastCacheUpdate;
    private static readonly TimeSpan CacheRefreshInterval = TimeSpan.FromMinutes(1);

    internal class PhysicalDisk
    {
        public string? MediaType { get; set; }
        public string? DeviceID { get; set; }
    }

    public static string GetDiskDriveFromLetter(string driveLetter)
    {
        try
        {
            var normalizedDriveLetter = driveLetter.Replace(@"\", "", StringComparison.Ordinal).ToUpperInvariant();

            // Refresh cache if needed
            EnsureCacheIsUpToDate();

            // Use cached data for disk and logical mappings
            if (cachedDiskMappings != null && cachedDiskMappings.TryGetValue(normalizedDriveLetter, out var deviceID))
            {
                var matchedDisk = cachedPhysicalDisks?.FirstOrDefault(disk => deviceID.Contains(disk.DeviceID ?? string.Empty, StringComparison.Ordinal));
                if (matchedDisk != null)
                {
                    return matchedDisk.MediaType switch
                    {
                        "3" => "HDD",
                        "4" => "SSD",
                        _ => "Unknown"
                    };
                }
            }
        }
        catch
        {
            // Handle any errors
        }

        return "Error";
    }

    private static void EnsureCacheIsUpToDate()
    {
        // Check if the cache is still valid (e.g., cache is older than the refresh interval)
        if (DateTime.Now - lastCacheUpdate > CacheRefreshInterval)
        {
            lock (cacheLock)
            {
                // Only refresh cache if another thread hasn't already updated it
                if (DateTime.Now - lastCacheUpdate > CacheRefreshInterval)
                {
                    RefreshCache();
                    lastCacheUpdate = DateTime.Now;
                }
            }
        }
    }

    private static void RefreshCache()
    {
        // Query the disk and logical mappings in one optimized WMI call
        var physicalDisksTask = Task.Run(GetPhysicalDisks);
        var logicalDiskMappingsTask = Task.Run(GetLogicalDiskMappings);

        Task.WhenAll(physicalDisksTask, logicalDiskMappingsTask).Wait();

        cachedPhysicalDisks = physicalDisksTask.Result;
        cachedDiskMappings = logicalDiskMappingsTask.Result;
    }

    private static List<PhysicalDisk> GetPhysicalDisks()
    {
        return [.. QueryWmiObjects("root\\Microsoft\\Windows\\Storage", "SELECT MediaType, DeviceID FROM MSFT_PhysicalDisk")
            .Select(obj => new PhysicalDisk
            {
                MediaType = obj["MediaType"]?.ToString(),
                DeviceID = obj["DeviceID"]?.ToString()
            })
            .Where(disk => !string.IsNullOrEmpty(disk.MediaType) && !string.IsNullOrEmpty(disk.DeviceID))];
    }

    private static Dictionary<string, string> GetLogicalDiskMappings()
    {
        var logicalDiskMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var diskDrives = QueryWmiObjects("SELECT DeviceID FROM Win32_DiskDrive");

        foreach (var drive in diskDrives)
        {
            var deviceID = drive["DeviceID"]?.ToString();
            if (string.IsNullOrEmpty(deviceID))
            {
                continue;
            }

            var partitions = GetAssociatedObjects("Win32_DiskDrive.DeviceID", deviceID, "Win32_DiskDriveToDiskPartition");
            foreach (var partition in partitions)
            {
                var partitionID = partition["DeviceID"]?.ToString();
                if (string.IsNullOrEmpty(partitionID))
                {
                    continue;
                }

                var disks = GetAssociatedObjects("Win32_DiskPartition.DeviceID", partitionID, "Win32_LogicalDiskToPartition");
                foreach (var disk in disks)
                {
                    var logicalDiskID = disk["DeviceID"]?.ToString();
                    if (!string.IsNullOrEmpty(logicalDiskID))
                    {
                        logicalDiskMappings[logicalDiskID.ToUpperInvariant()] = deviceID;
                    }
                }
            }
        }

        return logicalDiskMappings;
    }

    private static List<ManagementObject> GetAssociatedObjects(string propertyName, string propertyValue, string associatedClass)
    {
        if (string.IsNullOrEmpty(propertyValue))
        {
            return [];
        }

        var query = $"ASSOCIATORS OF {{{propertyName}='{propertyValue}'}} WHERE AssocClass={associatedClass}";
        return QueryWmiObjects(query);
    }

    private static List<ManagementObject> QueryWmiObjects(string scope, string query)
    {
        using var searcher = new ManagementObjectSearcher(scope, query);
        return [.. searcher.Get().Cast<ManagementObject>()];
    }

    private static List<ManagementObject> QueryWmiObjects(string query)
    {
        using var searcher = new ManagementObjectSearcher(query);
        return [.. searcher.Get().Cast<ManagementObject>()];
    }
}