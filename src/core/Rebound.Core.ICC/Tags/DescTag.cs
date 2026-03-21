// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Text;

namespace Rebound.Core.ICC.Tags;

/// <summary>
/// Desc (description/title) tag for ICC profiles.
/// </summary>
public static class DescTag
{
    public static byte[] Build(string text)
    {
        var ascii = Encoding.ASCII.GetBytes(text);
        var buf = new byte[4 + 4 + 4 + ascii.Length + 4 + 4 + 2 + 1 + 67];
        var pos = 0;

        void W32(uint v)
        {
            buf[pos++] = (byte)(v >> 24); buf[pos++] = (byte)(v >> 16);
            buf[pos++] = (byte)(v >> 8); buf[pos++] = (byte)v;
        }

        W32(0x64657363);           // 'desc'
        W32(0);                    // reserved
        W32((uint)ascii.Length);   // ASCII length including null
        Array.Copy(ascii, 0, buf, pos, ascii.Length);
        pos += ascii.Length;
        W32(0);                    // Unicode language code
        W32(0);                    // Unicode string length: 0
        buf[pos++] = 0;            // Mac script code high
        buf[pos++] = 0;            // Mac script code low
        buf[pos++] = 0;            // Mac string length
        pos += 67;                 // Mac string (zeroed)

        return buf;
    }
}
