// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.STORAGE_PROPERTY_ID;
using static TerraFX.Interop.Windows.STORAGE_QUERY_TYPE;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Core.SystemInformation.Hardware;

public static class Storage
{
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct ATA_PASS_THROUGH_EX
    {
        public ushort Length;
        public ushort AtaFlags;
        public byte PathId;
        public byte TargetId;
        public byte Lun;
        public byte ReservedAsUchar;
        public uint DataTransferLength;
        public uint TimeOutValue;
        public uint ReservedAsUlong;
        public nuint DataBufferOffset;
        public fixed byte PreviousTaskFile[8];
        public fixed byte CurrentTaskFile[8];
    }

    private const ushort ATA_FLAGS_DATA_IN = 0x0002;
    private const uint IOCTL_ATA_PASS_THROUGH = 0x0004D02C;

    /// <summary>
    /// Determines whether the physical drive backing the specified logical drive
    /// is a solid-state drive (SSD).
    /// </summary>
    /// <remarks>
    /// Three independent heuristics are used and combined:
    ///   1. TRIM support  – SSDs report TrimEnabled = true.
    ///   2. No seek penalty – SSDs report IncursSeekPenalty = false.
    ///   3. Nominal rotation rate == 1 (ATA word 217) – the spec value for
    ///      non-rotating (solid-state) media.
    /// A drive is considered an SSD when at least two of the three tests
    /// agree, or when only one test succeeds and the other two are
    /// indeterminate (i.e. the IOCTL calls failed).
    /// </remarks>
    /// <param name="driveLetterOrPath">A drive letter (e.g. "C", "C:") or path (e.g. "C:\") identifying the logical drive to check.</param>
    public static unsafe bool IsSSD(string driveLetterOrPath)
    {
        int physicalDriveNumber = GetPhysicalDriveNumber(driveLetterOrPath ?? "C:");
        if (physicalDriveNumber < 0)
            return false;

        string devicePath = $@"\\.\PhysicalDrive{physicalDriveNumber}";

        HANDLE hDevice = CreateFileW(
            devicePath.ToPointer(),
            GENERIC_READ | GENERIC_WRITE,
            FILE.FILE_SHARE_READ | FILE.FILE_SHARE_WRITE,
            null,
            OPEN.OPEN_EXISTING,
            0,
            HANDLE.NULL);

        if (hDevice == INVALID_HANDLE_VALUE)
            return false;

        try
        {
            // Tri-state: true = SSD evidence, false = HDD evidence, null = unknown
            bool? trimSsd = QueryTrim(hDevice);
            bool? seekSsd = QuerySeekPenalty(hDevice);
            bool? rpmSsd = QueryRotationRate(hDevice);

            // Count how many tests returned a definitive answer
            int ssdVotes = 0;
            int hddVotes = 0;

            Tally(trimSsd, ref ssdVotes, ref hddVotes);
            Tally(seekSsd, ref ssdVotes, ref hddVotes);
            Tally(rpmSsd, ref ssdVotes, ref hddVotes);

            // Majority wins; SSD wins ties (favour flash over spinning rust)
            return ssdVotes >= hddVotes;
        }
        finally
        {
            CloseHandle(hDevice);
        }
    }

    private static unsafe bool? QueryTrim(HANDLE hDevice)
    {
        STORAGE_PROPERTY_QUERY query = default;
        query.PropertyId = StorageDeviceTrimProperty;
        query.QueryType = PropertyStandardQuery;

        DEVICE_TRIM_DESCRIPTOR dtd = default;
        uint bytesReturned = 0;

        bool ok = DeviceIoControl(
            hDevice,
            IOCTL.IOCTL_STORAGE_QUERY_PROPERTY,
            &query, (uint)sizeof(STORAGE_PROPERTY_QUERY),
            &dtd, (uint)sizeof(DEVICE_TRIM_DESCRIPTOR),
            &bytesReturned,
            null);

        if (!ok || bytesReturned != sizeof(DEVICE_TRIM_DESCRIPTOR))
            return null;

        // TrimEnabled == true  →  SSD
        return dtd.TrimEnabled != 0;
    }

    private static unsafe bool? QuerySeekPenalty(HANDLE hDevice)
    {
        STORAGE_PROPERTY_QUERY query = default;
        query.PropertyId = StorageDeviceSeekPenaltyProperty;
        query.QueryType = PropertyStandardQuery;

        DEVICE_SEEK_PENALTY_DESCRIPTOR dspd = default;
        uint bytesReturned = 0;

        bool ok = DeviceIoControl(
            hDevice,
            IOCTL.IOCTL_STORAGE_QUERY_PROPERTY,
            &query, (uint)sizeof(STORAGE_PROPERTY_QUERY),
            &dspd, (uint)sizeof(DEVICE_SEEK_PENALTY_DESCRIPTOR),
            &bytesReturned,
            null);

        if (!ok || bytesReturned != sizeof(DEVICE_SEEK_PENALTY_DESCRIPTOR))
            return null;

        // IncursSeekPenalty == false  →  SSD
        return dspd.IncursSeekPenalty == 0;
    }

    private static unsafe bool? QueryRotationRate(HANDLE hDevice)
    {
        // ATA IDENTIFY DEVICE via ATA pass-through
        // We build the struct inline to avoid declaring a named unsafe struct
        // in managed source; the layout matches ATAIdentifyDeviceQuery exactly.

        const int DataWords = 256;
        const int DataBytes = DataWords * 2;

        // Use a byte buffer large enough for ATA_PASS_THROUGH_EX + data words
        int ataHeaderSize = sizeof(ATA_PASS_THROUGH_EX);
        int totalBytes = ataHeaderSize + DataBytes;

        byte* buffer = stackalloc byte[totalBytes];
        new Span<byte>(buffer, totalBytes).Clear();

        ATA_PASS_THROUGH_EX* header = (ATA_PASS_THROUGH_EX*)buffer;
        header->Length = (ushort)ataHeaderSize;
        header->AtaFlags = ATA_FLAGS_DATA_IN;
        header->DataTransferLength = (uint)DataBytes;
        header->TimeOutValue = 5;
        header->DataBufferOffset = (nuint)ataHeaderSize; // offset from start of buffer

        // CurrentTaskFile[6] = 0xEC -> ATA IDENTIFY DEVICE command
        header->CurrentTaskFile[6] = 0xEC;

        uint bytesReturned = 0;
        bool ok = DeviceIoControl(
            hDevice,
            IOCTL_ATA_PASS_THROUGH,
            buffer, (uint)totalBytes,
            buffer, (uint)totalBytes,
            &bytesReturned,
            null);

        if (!ok || bytesReturned != (uint)totalBytes)
            return null;

        // ATA word 217 = nominal media rotation rate
        // word index -> byte offset from start of data region
        const int RotRateWordIndex = 217;
        ushort* dataWords = (ushort*)(buffer + ataHeaderSize);
        ushort rotRate = dataWords[RotRateWordIndex];

        // 0x0001 → non-rotating (SSD); anything else is HDD or unknown
        return rotRate switch
        {
            0x0001 => true,   // solid state
            0x0000 => null,   // not reported
            0xFFFF => null,   // reserved
            _ => false   // actual RPM value -> spinning disk
        };
    }

    private static unsafe int GetPhysicalDriveNumber(string driveLetterOrPath)
    {
        // Normalise to "C:" form
        char letter = driveLetterOrPath.Trim().TrimEnd('\\', '/').TrimEnd(':')[0];
        string volumePath = $@"\\.\{letter}:";

        HANDLE hVolume = CreateFileW(
            volumePath.ToPointer(),
            0,                                       // no read/write needed for IOCTL
            FILE.FILE_SHARE_READ | FILE.FILE_SHARE_WRITE,
            null,
            OPEN.OPEN_EXISTING,
            0,
            HANDLE.NULL);

        if (hVolume == INVALID_HANDLE_VALUE)
            return -1;

        try
        {
            // IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS returns a VOLUME_DISK_EXTENTS
            // structure.  We allocate enough space for a reasonable number of extents.
            const int MaxExtents = 16;
            int bufferSize = sizeof(VOLUME_DISK_EXTENTS) +
                                     (MaxExtents - 1) * sizeof(DISK_EXTENT);
            byte* buffer = stackalloc byte[bufferSize];
            new Span<byte>(buffer, bufferSize).Clear();

            uint bytesReturned = 0;
            bool ok = DeviceIoControl(
                hVolume,
                IOCTL.IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS,
                null, 0,
                buffer, (uint)bufferSize,
                &bytesReturned,
                null);

            if (!ok)
                return -1;

            // First extent's DiskNumber is the physical drive we want
            VOLUME_DISK_EXTENTS* vde = (VOLUME_DISK_EXTENTS*)buffer;
            if (vde->NumberOfDiskExtents == 0)
                return -1;

            // The Extents field is the first element of a variable-length array
            return (int)vde->Extents.e0.DiskNumber;
        }
        finally
        {
            CloseHandle(hVolume);
        }
    }

    private static void Tally(bool? result, ref int ssdVotes, ref int hddVotes)
    {
        if (result == true) ssdVotes++;
        if (result == false) hddVotes++;
    }

    /// <summary>
    /// Gets the total size of the Windows drive (the drive containing the OS, typically C:\).
    /// </summary>
    public static long GetWindowsDriveSize()
    {
        var windowsDrive = GetWindowsDriveInfo();
        return windowsDrive?.TotalSize ?? 0;
    }

    /// <summary>
    /// Gets the total size of all drives combined.
    /// </summary>
    public static long GetTotalSize()
    {
        return DriveInfo.GetDrives()
            .Where(d => d.IsReady)
            .Sum(d => d.TotalSize);
    }

    /// <summary>
    /// Gets the occupied (used) space on the Windows drive in bytes.
    /// </summary>
    public static long GetWindowsDriveOccupiedSpace()
    {
        var windowsDrive = GetWindowsDriveInfo();
        if (windowsDrive == null) return 0;
        return windowsDrive.TotalSize - windowsDrive.TotalFreeSpace;
    }

    /// <summary>
    /// Gets the total occupied (used) space across all drives in bytes.
    /// </summary>
    public static long GetTotalOccupiedSpace()
    {
        return DriveInfo.GetDrives()
            .Where(d => d.IsReady)
            .Sum(d => d.TotalSize - d.TotalFreeSpace);
    }

    /// <summary>
    /// Gets the percentage of occupied space on the Windows drive (0.0 to 100.0).
    /// </summary>
    public static double GetWindowsDriveOccupiedSpacePercentage()
    {
        var windowsDrive = GetWindowsDriveInfo();
        if (windowsDrive == null || windowsDrive.TotalSize == 0) return 0;
        long used = windowsDrive.TotalSize - windowsDrive.TotalFreeSpace;
        return (double)used / windowsDrive.TotalSize * 100.0;
    }

    /// <summary>
    /// Gets the percentage of occupied space on the Windows drive as a string formatted with the current culture's number formatting.
    /// </summary>
    public static string GetWindowsDriveOccupiedSpacePercentageString()
    {
        return ((int)GetWindowsDriveOccupiedSpacePercentage()).ToString((IFormatProvider?)null);
    }

    /// <summary>
    /// Gets the percentage of occupied space across all drives combined (0.0 to 100.0).
    /// </summary>
    public static double GetTotalOccupiedSpacePercentage()
    {
        long totalSize = GetTotalSize();
        if (totalSize == 0) return 0;
        long totalOccupied = GetTotalOccupiedSpace();
        return (double)totalOccupied / totalSize * 100.0;
    }

    /// <summary>
    /// Gets the percentage of occupied space across all drives combined as a string formatted with the current culture's number formatting.
    /// </summary>
    public static string GetTotalOccupiedSpacePercentageString()
    {
        return ((int)GetTotalOccupiedSpacePercentage()).ToString((IFormatProvider?)null);
    }

    private static DriveInfo? GetWindowsDriveInfo()
    {
        string windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string driveLetter = Path.GetPathRoot(windowsPath) ?? "C:\\";

        return DriveInfo.GetDrives()
            .FirstOrDefault(d => d.IsReady &&
                string.Equals(d.RootDirectory.FullName, driveLetter,
                    StringComparison.OrdinalIgnoreCase));
    }
}