using System.Collections.Generic;
using System.Linq;
using System.Management;

#nullable enable

namespace ReboundDefrag.Helpers
{
    public class VolumeInfo
    {
        public string? GUID { get; set; }
        public string? FileSystem { get; set; }
        public ulong Size { get; set; }
        public string? FriendlyName { get; set; }
    }

    public class SystemVolumes
    {
        public static List<VolumeInfo> GetSystemVolumes()
        {
            List<VolumeInfo> volumes = [];

            // WMI query to get all volumes, including GUID paths
            string query = "SELECT * FROM Win32_Volume WHERE DriveLetter IS NULL";
            using (ManagementObjectSearcher searcher = new(query))
            {
                foreach (ManagementObject volume in searcher.Get().Cast<ManagementObject>())
                {
                    string? volumePath = volume["DeviceID"].ToString(); // This gives the \\?\Volume{GUID} path
                    string fileSystem = volume["FileSystem"]?.ToString() ?? "Unknown";
                    ulong size = (ulong)volume["Capacity"];

                    // We can further refine this by querying for EFI, Recovery, etc., based on size and file system
                    string friendlyName;
                    if (fileSystem == "FAT32" && size < 512 * 1024 * 1024)
                    {
                        friendlyName = "EFI System Partition";
                    }
                    else if (fileSystem == "NTFS" && size > 500 * 1024 * 1024)
                    {
                        friendlyName = "Recovery Partition";
                    }
                    else if (fileSystem == "NTFS" && size < 500 * 1024 * 1024)
                    {
                        friendlyName = "System Reserved Partition";
                    }
                    else
                    {
                        friendlyName = "Unknown System Partition";
                    }

                    volumes.Add(new VolumeInfo
                    {
                        GUID = volumePath,
                        FileSystem = fileSystem,
                        Size = size,
                        FriendlyName = friendlyName
                    });
                }
            }

            return volumes;
        }
    }
}
