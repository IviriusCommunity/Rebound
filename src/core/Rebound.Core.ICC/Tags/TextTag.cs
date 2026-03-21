// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Text;

namespace Rebound.Core.ICC.Tags;

/// <summary>
/// Text ("real" description) tag for ICC profiles.
/// </summary>
public static class TextTag
{
    public static byte[] Build(string text)
    {
        var ascii = Encoding.ASCII.GetBytes(text);
        var buf = new byte[4 + 4 + ascii.Length];
        var pos = 0;

        buf[pos++] = 0x74; buf[pos++] = 0x65; buf[pos++] = 0x78; buf[pos++] = 0x74; // 'text'
        buf[pos++] = 0; buf[pos++] = 0; buf[pos++] = 0; buf[pos++] = 0;             // reserved
        Array.Copy(ascii, 0, buf, pos, ascii.Length);

        return buf;
    }
}
