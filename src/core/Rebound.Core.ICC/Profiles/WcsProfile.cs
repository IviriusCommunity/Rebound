// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.ICC.Curves;
using Rebound.Core.ICC.Tags;

namespace Rebound.Core.ICC.Profiles;

/// <summary>
/// WCS profile generator class.
/// </summary>
public static class WcsProfile
{
    public static byte[] Generate(string title, string description, double gamma, double brightness = 0.0, double contrast = 1.0)
        => GeneratePerChannel(title, description, gamma, gamma, gamma, brightness, contrast);

    public static byte[] GeneratePerChannel(
        string title, string description,
        double redGamma, double greenGamma, double blueGamma,
        double brightness = 0.0, double contrast = 1.0)
    {
        var now = DateTime.UtcNow;

        // Build all tag data first so we know sizes
        var descData = DescTag.Build(title + "\0");
        var cprtData = TextTag.Build("Copyright (c) Ivirius Community 2020-present\0");
        var wtptData = XyzTag.Build(0x0000F354, 0x00010000, 0x000116C9);
        var lumiData = XyzTag.Build(0x00000000, 0x00005000, 0x00000000);
        var rXyzData = XyzTag.Build(0x00006F7B, 0x000038C3, 0x00000374);
        var gXyzData = XyzTag.Build(0x00006378, 0x0000B8D3, 0x00001682);
        var bXyzData = XyzTag.Build(0x000023E4, 0x00000E6A, 0x0000B936);
        var rTrcData = CurvTag.Build(GammaCurve.Forward(redGamma));
        var gTrcData = CurvTag.Build(GammaCurve.Forward(greenGamma));
        var bTrcData = CurvTag.Build(GammaCurve.Forward(blueGamma));
        var ms10Data = Ms10Tag.Build(title, description, redGamma, brightness, contrast, now);

        var tags = new (uint sig, byte[] data)[]
        {
            (0x64657363, descData),  // desc
            (0x63707274, cprtData),  // cprt
            (0x77747074, wtptData),  // wtpt
            (0x6C756D69, lumiData),  // lumi
            (0x7258595A, rXyzData),  // rXYZ
            (0x6758595A, gXyzData),  // gXYZ
            (0x6258595A, bXyzData),  // bXYZ
            (0x72545243, rTrcData),  // rTRC
            (0x67545243, gTrcData),  // gTRC
            (0x62545243, bTrcData),  // bTRC
            (0x4D533030, ms10Data),  // MS00
        };

        // Compute tag table and offsets
        // Header = 128, tag count = 4, tag table = 11 * 12 = 132
        var tagTableSize = 4 + tags.Length * 12;
        var currentOffset = (uint)(128 + tagTableSize);
        var offsets = new uint[tags.Length];
        for (var i = 0; i < tags.Length; i++)
        {
            offsets[i] = currentOffset;
            currentOffset += (uint)((tags[i].data.Length + 3) & ~3);
        }
        var totalSize = currentOffset;

        var buf = new byte[totalSize];
        var pos = 0;

        void W32(uint v)
        {
            buf[pos++] = (byte)(v >> 24); buf[pos++] = (byte)(v >> 16);
            buf[pos++] = (byte)(v >> 8); buf[pos++] = (byte)v;
        }
        void W16(ushort v)
        {
            buf[pos++] = (byte)(v >> 8); buf[pos++] = (byte)v;
        }

        // ── Header (128 bytes) ────────────────────────────────────────────
        W32(totalSize);          // file size
        W32(0x6C696E6F);         // CMM: 'lino'
        W32(0x02200000);         // ICC version 2.2
        W32(0x6D6E7472);         // class: 'mntr'
        W32(0x52474220);         // color space: 'RGB '
        W32(0x58595A20);         // PCS: 'XYZ '
        W16((ushort)now.Year);
        W16((ushort)now.Month);
        W16((ushort)now.Day);
        W16((ushort)now.Hour);
        W16((ushort)now.Minute);
        W16((ushort)now.Second);
        W32(0x61637370);         // 'acsp'
        W32(0x4D534654);         // platform: 'MSFT'
        W32(0);                  // flags
        W32(0);                  // device manufacturer
        W32(0);                  // device model
        // device attributes (8 bytes)
        buf[pos++] = 0; buf[pos++] = 0; buf[pos++] = 0; buf[pos++] = 0;
        buf[pos++] = 0; buf[pos++] = 0; buf[pos++] = 0; buf[pos++] = 0;
        W32(0);                  // rendering intent: perceptual
        W32(0x0000F6D6);         // illuminant X (D50)
        W32(0x00010000);         // illuminant Y (D50)
        W32(0x0000D32D);         // illuminant Z (D50)
        W32(0x49565243);         // creator: 'IVRC'
        pos += 16;               // MD5 (zeroed)
        pos += 28;               // reserved (zeroed)
        // pos == 128 here

        // ── Tag count ─────────────────────────────────────────────────────
        W32((uint)tags.Length);

        // ── Tag table ─────────────────────────────────────────────────────
        for (var i = 0; i < tags.Length; i++)
        {
            W32(tags[i].sig);
            W32(offsets[i]);
            W32((uint)tags[i].data.Length);
        }

        // ── Tag data ──────────────────────────────────────────────────────
        foreach (var (_, data) in tags)
        {
            Array.Copy(data, 0, buf, pos, data.Length);
            pos += data.Length;
            var pad = (4 - (data.Length % 4)) % 4;
            pos += pad; // already zeroed
        }

        return buf;
    }
}