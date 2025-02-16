using System.Collections.Generic;
using System.Text;
using Rebound.Helpers;

namespace Rebound.Cleanup.Helpers;

public partial class DriveComboBoxItem
{
    public string DriveName { get; set; }

    public string DrivePath { get; set; }

    public string ImagePath { get; set; }

    public string MediaType { get; set; }
}

public partial class DriveHelper
{
    public static string GetDriveTypeDescription(string driveRoot)
    {
        var driveType = Win32Helper.GetDriveType(driveRoot);

        return driveType switch
        {
            Win32Helper.DriveType.DRIVE_REMOVABLE => "Removable",
            Win32Helper.DriveType.DRIVE_FIXED => "Fixed",
            Win32Helper.DriveType.DRIVE_REMOTE => "Network",
            Win32Helper.DriveType.DRIVE_CDROM => "CD-ROM",
            Win32Helper.DriveType.DRIVE_RAMDISK => "RAM Disk",
            Win32Helper.DriveType.DRIVE_NO_ROOT_DIR => "No Root Directory",
            _ => "Unknown",
        };
    }

    public static List<DriveComboBoxItem> GetDriveItems()
    {
        // The drive items
        List<DriveComboBoxItem> items = [];

        // Get the logical drives bitmask
        var drivesBitMask = Win32Helper.GetLogicalDrives();

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

                // Create string builders
                StringBuilder volumeName = new(261);
                StringBuilder fileSystemName = new(261);

                // Obtain volume information using P/Invoke
                if (Win32Helper.GetVolumeInformation(drive, volumeName, volumeName.Capacity, out _, out _, out _, fileSystemName, fileSystemName.Capacity))
                {
                    // Convert singular drive letter of format C into drive letter of format C:
                    var driveLetter = $"{singularDriveLetter}:";

                    // Obtain the drive media type for drive path
                    var mediaType = GetDriveTypeDescription(drive);

                    // Create the drive item
                    var item = new DriveComboBoxItem
                    {
                        // Set the friendly name of format Local Disk (C:)
                        DriveName = volumeName.ToString() == string.Empty ? $"({driveLetter})" : $"{volumeName} ({driveLetter})",
                        ImagePath = "ms-appx:///Assets/Drive.png",
                        MediaType = mediaType,
                        DrivePath = drive,
                    };

                    // Set the icon for the drive
                    item.ImagePath = item.MediaType switch
                    {
                        "Removable" => "ms-appx:///Assets/DriveRemovable.png",
                        "Unknown" => "ms-appx:///Assets/DriveUnknown.png",
                        "CD-ROM" => "ms-appx:///Assets/DriveOptical.png",
                        _ => "ms-appx:///Assets/Drive.png"
                    };

                    // If the drive is the Windows installation drive use the Windows drive icon
                    item.ImagePath = driveLetter == EnvironmentHelper.GetWindowsInstallationDrivePath().DrivePathToLetter() ? "ms-appx:///Assets/DriveWindows.png" : item.ImagePath;

                    // Add to collection
                    items.Add(item);
                }
            }
        }

        // Return the drives list
        return items;
    }
}