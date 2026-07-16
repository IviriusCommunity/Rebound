// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Rebound.Cleanup.DiskAnalyzer.Ntfs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct NtfsBootSector
{
    public fixed byte Jump[3];          // 0x00
    public fixed byte OemId[8];         // 0x03

    public ushort BytesPerSector;       // 0x0B
    public byte SectorsPerCluster;      // 0x0D
    public ushort ReservedSectors;      // 0x0E

    public fixed byte Unused0[3];       // 0x10
    public ushort Unused1;              // 0x13

    public byte MediaDescriptor;        // 0x15
    public ushort Unused2;              // 0x16

    public ushort SectorsPerTrack;      // 0x18
    public ushort NumberOfHeads;        // 0x1A
    public uint HiddenSectors;          // 0x1C

    public uint Unused3;                // 0x20
    public uint Unused4;                // 0x24

    public ulong TotalSectors;          // 0x28
    public ulong MftLcn;                // 0x30
    public ulong MftMirrorLcn;          // 0x38

    public sbyte ClustersPerFileRecord; // 0x40
    public fixed byte Reserved2[3];     // 0x41

    public sbyte ClustersPerIndexBuffer;// 0x44
    public fixed byte Reserved3[3];     // 0x45

    public ulong VolumeSerialNumber;    // 0x48
    public uint Checksum;               // 0x50

    // Properties
    public readonly uint BytesPerCluster => (uint)(BytesPerSector * SectorsPerCluster);
    public readonly ulong MftOffset => MftLcn * BytesPerCluster;

    public readonly int FileRecordSize =>
        ClustersPerFileRecord > 0
            ? ClustersPerFileRecord * (int)BytesPerCluster
            : 1 << -ClustersPerFileRecord;

    public readonly bool IsNtfs
    {
        get
        {
            fixed (byte* p = OemId)
            {
                return p[0] == 'N' && p[1] == 'T' && p[2] == 'F' && p[3] == 'S' &&
                       p[4] == ' ' && p[5] == ' ' && p[6] == ' ' && p[7] == ' ';
            }
        }
    }
}