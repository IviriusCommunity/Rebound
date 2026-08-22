// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.ICC.Curves;
using Rebound.Core.ICC.Tags;

namespace Rebound.Core.ICC.Profiles;

public static class WcsProfile
{
    public static byte[] Generate(string title, string description, double gamma, double brightness = 0.0, double gain = 1.0)
        => GeneratePerChannel(title, description, gamma, gamma, gamma, gain, gain, gain, brightness);

    public static byte[] GeneratePerChannel(
        string title, string description,
        double redGamma, double greenGamma, double blueGamma,
        double redGain = 1.0, double greenGain = 1.0, double blueGain = 1.0,
        double brightness = 0.0)
    {
        var now = DateTime.UtcNow;

        var descData = DescTag.Build(title + "\0");
        var cprtData = TextTag.Build("Copyright (c) Ivirius Community 2020-present\0");
        var wtptData = XyzTag.Build(0x0000F354, 0x00010000, 0x000116C9);
        var lumiData = XyzTag.Build(0x00000000, 0x00005000, 0x00000000);
        var rXyzData = XyzTag.Build(0x00006F7B, 0x000038C3, 0x00000374);
        var gXyzData = XyzTag.Build(0x00006378, 0x0000B8D3, 0x00001682);
        var bXyzData = XyzTag.Build(0x000023E4, 0x00000E6A, 0x0000B936);

        // Pass both gamma AND per-channel gain to GammaCurve constructor
        var rTrcData = CurvTag.Build(new GammaCurve(redGamma, brightness, redGain));
        var gTrcData = CurvTag.Build(new GammaCurve(greenGamma, brightness, greenGain));
        var bTrcData = CurvTag.Build(new GammaCurve(blueGamma, brightness, blueGain));

        var ms10Data = Ms10Tag.Build(title, description, redGamma, redGain, greenGain, blueGain, brightness, now);

        var tags = new (uint sig, byte[] data)[]
        {
            (0x64657363, descData),
            (0x63707274, cprtData),
            (0x77747074, wtptData),
            (0x6C756D69, lumiData),
            (0x7258595A, rXyzData),
            (0x6758595A, gXyzData),
            (0x6258595A, bXyzData),
            (0x72545243, rTrcData),
            (0x67545243, gTrcData),
            (0x62545243, bTrcData),
            (0x4D533030, ms10Data),
        };

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

        W32(totalSize);
        W32(0x6C696E6F);
        W32(0x02200000);
        W32(0x6D6E7472);
        W32(0x52474220);
        W32(0x58595A20);
        W16((ushort)now.Year);
        W16((ushort)now.Month);
        W16((ushort)now.Day);
        W16((ushort)now.Hour);
        W16((ushort)now.Minute);
        W16((ushort)now.Second);
        W32(0x61637370);
        W32(0x4D534654);
        W32(0);
        W32(0);
        W32(0);
        buf[pos++] = 0; buf[pos++] = 0; buf[pos++] = 0; buf[pos++] = 0;
        buf[pos++] = 0; buf[pos++] = 0; buf[pos++] = 0; buf[pos++] = 0;
        W32(0);
        W32(0x0000F6D6);
        W32(0x00010000);
        W32(0x0000D32D);
        W32(0x49565243);
        pos += 16;
        pos += 28;

        W32((uint)tags.Length);

        for (var i = 0; i < tags.Length; i++)
        {
            W32(tags[i].sig);
            W32(offsets[i]);
            W32((uint)tags[i].data.Length);
        }

        foreach (var (_, data) in tags)
        {
            Array.Copy(data, 0, buf, pos, data.Length);
            pos += data.Length;
            var pad = (4 - (data.Length % 4)) % 4;
            pos += pad;
        }

        return buf;
    }
}