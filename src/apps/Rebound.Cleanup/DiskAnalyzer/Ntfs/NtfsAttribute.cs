// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Rebound.Cleanup.DiskAnalyzer.Ntfs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct NtfsAttributeHeader
{
    public uint Type;            // e.g., 0x30 ($FILE_NAME) or 0x80 ($DATA)
    public uint Length;          // Length of this entire attribute block
    public byte NonResident;     // 0 = Resident in record, 1 = Allocates external clusters
    public byte NameLength;
    public ushort NameOffset;
    public ushort Flags;
    public ushort AttributeId;
}

internal enum NtfsAttributeType : uint
{
    StandardInformation = 0x10,
    AttributeList = 0x20,
    FileName = 0x30,
    ObjectId = 0x40,
    SecurityDescriptor = 0x50,
    VolumeName = 0x60,
    VolumeInformation = 0x70,
    Data = 0x80,
    IndexRoot = 0x90,
    IndexAllocation = 0xA0,
    Bitmap = 0xB0,
    ReparsePoint = 0xC0,
    EaInformation = 0xD0,
    Ea = 0xE0,
    LoggedUtilityStream = 0x100,

    End = 0xFFFFFFFF
}

internal unsafe ref struct NtfsAttributeEnumerator
{
    private NtfsAttributeHeader* _current;
    private readonly byte* _end;

    public NtfsAttributeHeader* Current;

    public NtfsAttributeEnumerator(byte* record, int recordSize)
    {
        NtfsFileRecordHeader* header = (NtfsFileRecordHeader*)record;

        _current = (NtfsAttributeHeader*)(record + header->FirstAttributeOffset);
        _end = record + recordSize;

        Current = null;
    }

    public bool MoveNext()
    {
        if (_current == null)
            return false;

        if ((byte*)_current >= _end)
            return false;

        if (_current->Type == (uint)NtfsAttributeType.End)
            return false;

        Current = _current;

        _current = (NtfsAttributeHeader*)((byte*)_current + _current->Length);

        return true;
    }
}