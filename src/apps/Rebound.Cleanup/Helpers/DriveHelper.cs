using System;
using System.Collections.ObjectModel;
using Rebound.Cleanup.Items;
using Rebound.Helpers;
using Windows.Win32;

namespace Rebound.Cleanup.Helpers;

internal static partial class DriveHelper
{
    public static string GetDriveTypeDescription(string driveRoot)
    {
        var driveType = PInvoke.GetDriveType(driveRoot);

        return driveType switch
        {
            2u => "Removable",         // DRIVE_REMOVABLE
            3u => "Fixed",             // DRIVE_FIXED
            4u => "Network",           // DRIVE_REMOTE
            5u => "CD-ROM",            // DRIVE_CDROM
            6u => "RAM Disk",          // DRIVE_RAMDISK
            1u => "No Root Directory", // DRIVE_NO_ROOT_DIR
            _ => "Unknown",
        };
    }

    public static unsafe ObservableCollection<DriveComboBoxItem> GetDriveItems()
    {
        // Initialize the collection of drive items to return
        ObservableCollection<DriveComboBoxItem> items = [];

        // Get a bitmask representing all logical drives
        var drivesBitMask = PInvoke.GetLogicalDrives();
        if (drivesBitMask == 0)
        {
            // No drives found, return empty list
            return items;
        }

        // Allocate stack space for volume and file system names
        Span<char> volumeName = stackalloc char[261];
        Span<char> fileSystemName = stackalloc char[261];

        uint serialNumber, maxComponentLength, fsFlags;

        // Iterate over all possible drive letters (A-Z)
        for (var letter = 'A'; letter <= 'Z'; letter++)
        {
            var mask = 1u << (letter - 'A');

            // Skip this letter if it's not set in the drive bitmask
            if ((drivesBitMask & mask) == 0)
            {
                continue;
            }

            // Format the full drive path (e.g., "C:\")
            var drivePath = $"{letter}:\\";

            // Attempt to retrieve volume information for the drive
            if (!PInvoke.GetVolumeInformation(drivePath, volumeName, &serialNumber, &maxComponentLength, &fsFlags, fileSystemName))
            {
                continue; // Skip if unable to read volume info
            }

            // Create a formatted drive letter (e.g., "C:")
            var driveLetter = $"{letter}:";

            // Determine the media type of the drive (e.g., "Removable", "CD-ROM", etc.)
            var mediaType = GetDriveTypeDescription(drivePath);

            // Create the display name for the drive
            var name = volumeName.IsEmpty ? $"({driveLetter})" : $"{volumeName} ({driveLetter})";

            // Select an icon based on media type
            var imagePath = mediaType switch
            {
                "Removable" => "ms-appx:///Assets/DriveRemovable.png",
                "CD-ROM" => "ms-appx:///Assets/DriveOptical.png",
                "Unknown" => "ms-appx:///Assets/DriveUnknown.png",
                _ => "ms-appx:///Assets/Drive.ico"
            };

            // Use a special icon if this is the Windows installation drive
            if (driveLetter == EnvironmentHelper.GetWindowsInstallationDrivePath().DrivePathToLetter())
            {
                imagePath = "ms-appx:///Assets/DriveWindows.ico";
            }

            // Add the fully constructed drive item to the collection
            items.Add(new DriveComboBoxItem
            {
                DriveName = name,
                ImagePath = imagePath,
                MediaType = mediaType,
                DrivePath = drivePath,
            });
        }

        // Return the populated collection of drive items
        return items;
    }
}