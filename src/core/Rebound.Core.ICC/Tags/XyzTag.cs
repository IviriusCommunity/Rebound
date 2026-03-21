// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Core.ICC.Tags;

/// <summary>
/// Xyz tag for ICC color profiles.
/// </summary>
public static class XyzTag
{
    public static byte[] Build(uint x, uint y, uint z)
    {
        var buf = new byte[20];
        var pos = 0;

        void W32(uint v)
        {
            buf[pos++] = (byte)(v >> 24); buf[pos++] = (byte)(v >> 16);
            buf[pos++] = (byte)(v >> 8); buf[pos++] = (byte)v;
        }

        W32(0x58595A20); // 'XYZ '
        W32(0);          // reserved
        W32(x);
        W32(y);
        W32(z);

        return buf;
    }
}
