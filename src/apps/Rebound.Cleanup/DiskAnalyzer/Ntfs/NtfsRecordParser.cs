// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Rebound.Cleanup.DiskAnalyzer.Ntfs;

internal static unsafe class NtfsRecordParser
{
    public static unsafe void WalkAttributes(
        byte* record,
        int recordSize,
        delegate*<NtfsAttributeHeader*, void> callback)
    {
        NtfsFileRecordHeader* header =
            (NtfsFileRecordHeader*)record;

        byte* p = record + header->FirstAttributeOffset;

        while (true)
        {
            NtfsAttributeHeader* attribute =
                (NtfsAttributeHeader*)p;

            if (attribute->Type == (uint)NtfsAttributeType.End)
                break;

            if (attribute->Length < sizeof(NtfsAttributeHeader))
                throw new InvalidDataException("Corrupt attribute length.");

            callback(attribute);

            p += attribute->Length;
        }
    }
}