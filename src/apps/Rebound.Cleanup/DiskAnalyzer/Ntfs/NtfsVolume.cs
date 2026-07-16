// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Native.Wrappers;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.FILE;
using static TerraFX.Interop.Windows.MEM;
using static TerraFX.Interop.Windows.OPEN;
using static TerraFX.Interop.Windows.PAGE;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Cleanup.DiskAnalyzer.Ntfs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct DiskNode
{
    public ulong ParentId;
    public ulong FirstChildId;  // O(1) Hierarchy Head
    public ulong NextSiblingId; // O(1) Sibling Link
    public long Size;
    public byte IsDirectory;
}

internal sealed unsafe class NtfsVolume : IDisposable
{
    public HANDLE _handle;

    public NtfsBootSector BootSector { get; }

    private NtfsVolume(HANDLE handle, NtfsBootSector bootSector)
    {
        _handle = handle;
        BootSector = bootSector;
    }

    public void EnumerateVolumeRecords(NtfsVolume volume)
    {
        int recordSize = volume.BootSector.FileRecordSize;
        byte* recordBuffer = stackalloc byte[recordSize];

        // An average drive might contain hundreds of thousands to millions of records
        // WizTree loop steps forward sequentially from record 0
        for (ulong recordId = 0; recordId < 5000000; recordId++)
        {
            try
            {
                volume.ReadRecordNative(recordId, recordBuffer);
                NtfsFileRecordHeader* header = (NtfsFileRecordHeader*)recordBuffer;

                if (header->Magic != 0x454C4946) // Skip empty/zeroed sectors ("FILE")
                    continue;

                // This record is active! Send it to the parsing parser logic
                ParseMftRecord(recordId, header);
            }
            catch (Exception)
            {
                // Reach the physical end of the MFT allocation stream bounds
                break;
            }
        }
    }

    private unsafe void ParseMftRecord(ulong recordId, NtfsFileRecordHeader* record)
    {
        // Point to the first attribute byte
        byte* attrPtr = (byte*)record + record->FirstAttributeOffset;

        ulong parentId = 0;
        long realSize = 0;

        while (true)
        {
            NtfsAttributeHeader* attr = (NtfsAttributeHeader*)attrPtr;
            if (attr->Type == 0xFFFFFFFF || attr->Length == 0)
                break; // End of file record space attributes

            if (attr->Type == 0x30) // $FILE_NAME
            {
                // Resident payload descriptor offset logic
                byte* residentData = attrPtr + *(ushort*)(attrPtr + 20);

                // Offset 0 of the File Name payload contains the 8-byte Parent Directory Record reference
                parentId = *(ulong*)residentData & 0xFFFFFFFFFFFF; // Mask off the 48-bit sequence number

                // Extract the filename string data from the payload here if building UI strings...
            }
            else if (attr->Type == 0x80) // $DATA
            {
                if (attr->NonResident == 0) // Resident inside this 1024-byte block
                {
                    realSize = *(uint*)(attrPtr + 16); // Data Size
                }
                else // Non-Resident (Large files whose clusters live outside the MFT block)
                {
                    realSize = *(long*)(attrPtr + 48); // Real Size of file content
                }
            }

            attrPtr += attr->Length; // Advance to next attribute boundary
        }

        // Push the parsed results straight into the fast tree index mapping array
        AddNodeToFlatTree(recordId, parentId, realSize, (record->Flags & 0x01) != 0);
    }

    // Blittable flat struct mapping entry for Native AOT optimization
    internal struct DiskNode
    {
        public ulong ParentId;
        public long Size;
        public bool IsDirectory;
    }

    // Global flat tracking index buffer array
    // 0x500000 elements accommodates roughly 5 million files tracking capability safely
    private static DiskNode* _flatTreeBuffer;

    public void InitializeTreeStorage()
    {
        // Native AOT unmanaged memory slab allocation - raw speeds
        nuint bytesRequired = (nuint)(5000000 * sizeof(DiskNode));
        _flatTreeBuffer = (DiskNode*)NativeMemory.AllocZeroed(bytesRequired);
    }

    public void AddNodeToFlatTree(ulong recordId, ulong parentId, long size, bool isDirectory)
    {
        if (recordId >= 5000000) return;

        _flatTreeBuffer[recordId].ParentId = parentId;
        _flatTreeBuffer[recordId].Size = size;
        _flatTreeBuffer[recordId].IsDirectory = isDirectory;
    }

    public void RollUpDirectorySizes()
    {
        // Traverse the flat buffer backwards! 
        // This allows child file size roll-ups to ripple upwards through parent directory structures instantly.
        for (int i = 5000000 - 1; i >= 0; i--)
        {
            ulong parent = _flatTreeBuffer[i].ParentId;
            long currentSize = _flatTreeBuffer[i].Size;

            // Ensure target boundary doesn't cross self-referential bounds or point to empty root indexes
            if (parent > 0 && parent < 5000000 && parent != (ulong)i)
            {
                // Accumulate sizes dynamically upward to the parent slot item entry
                _flatTreeBuffer[parent].Size += currentSize;
            }
        }
    }

    // FSCTL_GET_NTFS_FILE_RECORD = 0x00090068
    private const uint FSCTL_GET_NTFS_FILE_RECORD = 0x00090068;

    public void ReadRecordNative(ulong recordNumber, byte* destinationBuffer)
    {
        NTFS_FILE_RECORD_INPUT_BUFFER input = new()
        {
            FileReferenceNumber = (long)recordNumber
        };

        // Buffer dimensions: Struct layout header (12 bytes) + MFT Record Size (1024 bytes)
        uint outputBufferSize = (uint)sizeof(NTFS_FILE_RECORD_OUTPUT_BUFFER) + (uint)BootSector.FileRecordSize;

        // Allocate entirely on the execution thread stack - No GC, No Blit Overhead
        byte* stackBuffer = stackalloc byte[(int)outputBufferSize];
        uint bytesReturned = 0;

        int success = DeviceIoControl(
            _handle,
            FSCTL_GET_NTFS_FILE_RECORD,
            &input,
            (uint)sizeof(NTFS_FILE_RECORD_INPUT_BUFFER),
            stackBuffer,
            outputBufferSize,
            &bytesReturned,
            null
        );

        if (success == 0)
            throw new Win32Exception((int)GetLastError());

        NTFS_FILE_RECORD_OUTPUT_BUFFER* outputHeader = (NTFS_FILE_RECORD_OUTPUT_BUFFER*)stackBuffer;

        // Calculate memory offset targeting the start of the raw record block payload
        byte* srcRecordPayload = stackBuffer + sizeof(NTFS_FILE_RECORD_OUTPUT_BUFFER);

        // Blast the memory segment across to your destination pointer
        Buffer.MemoryCopy(srcRecordPayload, destinationBuffer, BootSector.FileRecordSize, outputHeader->FileRecordLength);
    }

    public static NtfsVolume Open(char driveLetter)
    {
        using ManagedPtr<char> path = $"\\\\.\\{driveLetter}:";

        HANDLE handle = CreateFileW(
            path,
            GENERIC_READ,
            FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
            null,
            OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL,
            HANDLE.NULL);

        if (handle == HANDLE.INVALID_VALUE)
            throw new Win32Exception((int)GetLastError());

        using ManagedArrayPtr<byte> sector = new(512);

        Read(handle, 0, sector);

        NtfsBootSector bootSector = *(NtfsBootSector*)sector.ObjectPointer;

        ReboundLogger.WriteToLog(
    "Offset check",
    $"Offset of ClustersPerFileRecord = {Marshal.OffsetOf<NtfsBootSector>(nameof(NtfsBootSector.ClustersPerFileRecord))}",
    LogMessageSeverity.Warning);

        if (!bootSector.IsNtfs)
        {
            CloseHandle(handle);
            throw new InvalidDataException("The volume is not formatted as NTFS.");
        }

        for (int i = 0x38; i <= 0x48; i++)
        {
            ReboundLogger.WriteToLog(
                "Boot",
                $"{i:X2}: {sector[i]:X2}",
                LogMessageSeverity.Warning);
        }

        return new NtfsVolume(handle, bootSector);
    }

    public void Dispose()
    {
        if (_handle != HANDLE.INVALID_VALUE)
        {
            CloseHandle(_handle);
            _handle = HANDLE.INVALID_VALUE;
        }
    }

    public ManagedArrayPtr<byte> ReadRecord(ulong recordNumber)
    {
        ulong offset = BootSector.MftOffset + (recordNumber * (ulong)BootSector.FileRecordSize);

        // 1. Allocate a page-aligned buffer using VirtualAlloc instead of standard allocation
        void* alignedBuffer = VirtualAlloc(null, (nuint)BootSector.FileRecordSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
        if (alignedBuffer == null)
            throw new Win32Exception((int)GetLastError());

        try
        {
            // 2. Wrap or cast your aligned pointer to pass to your Read implementation
            // Note: You will need to make sure your Read method accepts a raw pointer 
            // or that ManagedArrayPtr can be constructed from an existing native pointer.

            // Read(_handle, offset, alignedBuffer, FileRecordSize); 

            // 3. Populate your managed wrapper object to return to the application
            ManagedArrayPtr<byte> record = new(BootSector.FileRecordSize);
            Buffer.MemoryCopy(alignedBuffer, record.ObjectPointer, BootSector.FileRecordSize, BootSector.FileRecordSize);

            return record;
        }
        finally
        {
            // 4. Free the aligned native memory
            VirtualFree(alignedBuffer, 0, MEM_RELEASE);
        }
    }

    private static void Read(
        HANDLE handle,
        ulong offset,
        ManagedArrayPtr<byte> buffer)
    {
        LARGE_INTEGER li = default;
        li.QuadPart = (long)offset;

        if (!SetFilePointerEx(handle, li, null, FILE_BEGIN))
            throw new Win32Exception((int)GetLastError());

        uint read;

        if (!ReadFile(handle, buffer, (uint)buffer.ByteLength, &read, null))
            throw new Win32Exception((int)GetLastError());

        if (read != buffer.ByteLength)
            throw new EndOfStreamException();
    }
}