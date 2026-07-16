// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Rebound.Cleanup.DiskAnalyzer.Ntfs;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct NtfsFileRecordHeader
{
    public uint Magic;              // "FILE"

    public ushort UsaOffset;
    public ushort UsaCount;

    public ulong Lsn;

    public ushort SequenceNumber;
    public ushort HardLinkCount;

    public ushort FirstAttributeOffset;
    public ushort Flags;

    public uint UsedSize;
    public uint AllocatedSize;

    public ulong BaseFileRecord;

    public ushort NextAttributeId;
    public ushort Reserved;

    public uint RecordNumber;
}