// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Rebound.Cleanup.DiskAnalyzer.Ntfs;

internal static class NtfsConstants
{
    public const uint FileMagic = 0x454C4946;

    public const uint BaadMagic = 0x44414142;

    public const uint HoleMagic = 0x454C4F48;

    public const uint ChkdMagic = 0x444B4843;
}