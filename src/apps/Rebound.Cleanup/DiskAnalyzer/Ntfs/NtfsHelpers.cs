// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Native.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Rebound.Cleanup.DiskAnalyzer.Ntfs;

internal static unsafe class NtfsHelpers
{
    public static void ApplyUsaFixup(
        ManagedArrayPtr<byte> record,
        ushort bytesPerSector)
    {
        NtfsFileRecordHeader* header =
            (NtfsFileRecordHeader*)record.ObjectPointer;

        ushort* usa =
            (ushort*)((byte*)record.ObjectPointer + header->UsaOffset);

        ushort sequenceNumber = usa[0];

        int sectorCount = header->UsaCount - 1;

        for (int i = 0; i < sectorCount; i++)
        {
            ushort* sectorTrailer =
                (ushort*)((byte*)record.ObjectPointer +
                ((i + 1) * bytesPerSector) - sizeof(ushort));

            if (*sectorTrailer != sequenceNumber)
            {
                throw new InvalidDataException(
                    $"USA verification failed for sector {i}.");
            }

            *sectorTrailer = usa[i + 1];
        }
    }
}