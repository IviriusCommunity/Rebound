// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.ICC.Curves;

namespace Rebound.Core.ICC.Tags;

/// <summary>
/// Curv tag for ICC color profiles.
/// </summary>
public static class CurvTag
{
    public static byte[] Build(GammaCurve curve)
    {
        // 4 sig + 4 reserved + 4 count + 256*2 values = 524 bytes
        var buf = new byte[524];
        var pos = 0;

        buf[pos++] = 0x63; buf[pos++] = 0x75; buf[pos++] = 0x72; buf[pos++] = 0x76; // 'curv'
        buf[pos++] = 0; buf[pos++] = 0; buf[pos++] = 0; buf[pos++] = 0;             // reserved
        buf[pos++] = 0; buf[pos++] = 0; buf[pos++] = 1; buf[pos++] = 0;             // count: 256

        foreach (var v in curve?.Values!)
        {
            buf[pos++] = (byte)(v >> 8);
            buf[pos++] = (byte)v;
        }

        return buf;
    }
}