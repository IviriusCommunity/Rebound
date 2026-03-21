// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.ICC.Curves;
using System.Text;

namespace Rebound.Core.ICC.Profiles;

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
        var descData = MakeDescTag(title + "\0");
        var cprtData = MakeTextTag("Copyright (c) Ivirius Community 2020-present\0");
        var wtptData = MakeXyzTag(0x0000F354, 0x00010000, 0x000116C9);
        var lumiData = MakeXyzTag(0x00000000, 0x00005000, 0x00000000);
        var rXyzData = MakeXyzTag(0x00006F7B, 0x000038C3, 0x00000374);
        var gXyzData = MakeXyzTag(0x00006378, 0x0000B8D3, 0x00001682);
        var bXyzData = MakeXyzTag(0x000023E4, 0x00000E6A, 0x0000B936);
        var rTrcData = MakeCurvTag(GammaCurve.Forward(redGamma));
        var gTrcData = MakeCurvTag(GammaCurve.Forward(greenGamma));
        var bTrcData = MakeCurvTag(GammaCurve.Forward(blueGamma));
        var ms10Data = MakeMs10Tag(title, description, redGamma, brightness, contrast, now);

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

    private static string EscapeXml(string s) => s
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("\"", "&quot;");

    // ── Tag builders ──────────────────────────────────────────────────────

    private static byte[] MakeDescTag(string text)
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

    private static byte[] MakeTextTag(string text)
    {
        var ascii = Encoding.ASCII.GetBytes(text);
        var buf = new byte[4 + 4 + ascii.Length];
        var pos = 0;

        buf[pos++] = 0x74; buf[pos++] = 0x65; buf[pos++] = 0x78; buf[pos++] = 0x74; // 'text'
        buf[pos++] = 0; buf[pos++] = 0; buf[pos++] = 0; buf[pos++] = 0;             // reserved
        Array.Copy(ascii, 0, buf, pos, ascii.Length);

        return buf;
    }

    private static byte[] MakeXyzTag(uint x, uint y, uint z)
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

    private static byte[] MakeCurvTag(GammaCurve curve)
    {
        // 4 sig + 4 reserved + 4 count + 256*2 values = 524 bytes
        var buf = new byte[524];
        var pos = 0;

        buf[pos++] = 0x63; buf[pos++] = 0x75; buf[pos++] = 0x72; buf[pos++] = 0x76; // 'curv'
        buf[pos++] = 0; buf[pos++] = 0; buf[pos++] = 0; buf[pos++] = 0;             // reserved
        buf[pos++] = 0; buf[pos++] = 0; buf[pos++] = 1; buf[pos++] = 0;             // count: 256

        foreach (var v in curve.Values)
        {
            buf[pos++] = (byte)(v >> 8);
            buf[pos++] = (byte)v;
        }

        return buf;
    }

    private static byte[] MakeMs10Tag(
        string title, string description,
        double gamma, double brightness, double contrast, DateTime now)
    {
        var dmpXml = BuildDmpXml(title, description, gamma, brightness, contrast, now);
        var campXml = BuildCampXml();
        var gmmpXml = BuildGmmpXml();

        var dmpBytes = Encoding.Unicode.GetBytes(dmpXml);
        var campBytes = Encoding.Unicode.GetBytes(campXml);
        var gmmpBytes = Encoding.Unicode.GetBytes(gmmpXml);

        // Header: sig(4) + reserved(4) + headerSize(4) + dmpSize(4) +
        //         dmpOffset(4) + campSize(4) + campOffset(4) + gmmpSize(4) = 32 bytes
        const int HEADER_SIZE = 32;
        var dmpOffset = (uint)HEADER_SIZE;
        var campOffset = (uint)(HEADER_SIZE + dmpBytes.Length);
        var gmmpOffset = (uint)(HEADER_SIZE + dmpBytes.Length + campBytes.Length);
        var totalSize = (int)(gmmpOffset + gmmpBytes.Length);

        var buf = new byte[totalSize];
        var pos = 0;

        void W32(uint v)
        {
            buf[pos++] = (byte)(v >> 24); buf[pos++] = (byte)(v >> 16);
            buf[pos++] = (byte)(v >> 8); buf[pos++] = (byte)v;
        }

        W32(0x4D533130);           // 'MS10'
        W32(0);                    // reserved
        W32(HEADER_SIZE);          // header size: 32
        W32((uint)dmpBytes.Length);
        W32(dmpOffset);
        W32((uint)campBytes.Length);
        W32(campOffset);
        W32((uint)gmmpBytes.Length);
        // GMMP offset is implicit (not stored)

        Array.Copy(dmpBytes, 0, buf, (int)dmpOffset, dmpBytes.Length);
        Array.Copy(campBytes, 0, buf, (int)campOffset, campBytes.Length);
        Array.Copy(gmmpBytes, 0, buf, (int)gmmpOffset, gmmpBytes.Length);

        return buf;
    }

    private static string BuildDmpXml(
        string title, string description,
        double gamma, double brightness, double contrast, DateTime now)
    {
        // WCS uses inverse gamma (1/gamma) in ParameterizedCurves
        var invGamma = 1.0 / gamma;
        var timestamp = now.ToString("yyyy-MM-ddTHH:mm:ss");

        var g = gamma.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
        var ig = invGamma.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
        var b = brightness.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
        var c = contrast.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);

        return
$@"<?xml version=""1.0"" encoding=""utf-16""?>
<cdm:ColorDeviceModel xmlns:cdm=""http://schemas.microsoft.com/windows/2005/02/color/ColorDeviceModel"" xmlns:cal=""http://schemas.microsoft.com/windows/2007/11/color/Calibration"" xmlns:wcs=""http://schemas.microsoft.com/windows/2005/02/color/WcsCommonProfileTypes"" xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"">
	<cdm:ProfileName>
		<wcs:Text xml:lang=""en-US"">{EscapeXml(title)}</wcs:Text>
	</cdm:ProfileName>
	<cdm:Description>
		<wcs:Text xml:lang=""en-US"">{EscapeXml(description)}</wcs:Text>
	</cdm:Description>
	<cdm:Author>
		<wcs:Text xml:lang=""en-US"">Ivirius Community</wcs:Text>
	</cdm:Author>
	<cdm:MeasurementConditions>
		<cdm:ColorSpace>CIEXYZ</cdm:ColorSpace>
		<cdm:WhitePointName>D65</cdm:WhitePointName>
	</cdm:MeasurementConditions>
	<cdm:SelfLuminous>true</cdm:SelfLuminous>
	<cdm:MaxColorant>1.0</cdm:MaxColorant>
	<cdm:MinColorant>0.0</cdm:MinColorant>
	<cdm:RGBVirtualDevice>
		<cdm:MeasurementData TimeStamp=""{timestamp}"">
			<cdm:MaxColorantUsed>1.0</cdm:MaxColorantUsed>
			<cdm:MinColorantUsed>0.0</cdm:MinColorantUsed>
			<cdm:WhitePrimary X=""95.05"" Y=""100.00"" Z=""108.90""/>
			<cdm:RedPrimary X=""41.24"" Y=""21.26"" Z=""1.93""/>
			<cdm:GreenPrimary X=""35.76"" Y=""71.52"" Z=""11.92""/>
			<cdm:BluePrimary X=""18.05"" Y=""7.22"" Z=""95.05""/>
			<cdm:BlackPrimary X=""0"" Y=""0"" Z=""0""/>
			<cdm:GammaOffsetGainLinearGain Gamma=""{g}"" Offset=""{b}"" Gain=""{c}"" LinearGain=""12.92"" TransitionPoint=""0.04045""/>
		</cdm:MeasurementData>
	</cdm:RGBVirtualDevice>
	<cdm:Calibration>
		<cal:AdapterGammaConfiguration>
			<cal:ParameterizedCurves>
				<wcs:RedTRC Gamma=""{ig}"" Gain=""{c}"" Offset1=""{b}""/>
				<wcs:GreenTRC Gamma=""{ig}"" Gain=""{c}"" Offset1=""{b}""/>
				<wcs:BlueTRC Gamma=""{ig}"" Gain=""{c}"" Offset1=""{b}""/>
			</cal:ParameterizedCurves>
		</cal:AdapterGammaConfiguration>
	</cdm:Calibration>
</cdm:ColorDeviceModel>";
    }

    private static string BuildCampXml() =>
@"<?xml version=""1.0""?>
<cam:ColorAppearanceModel ID=""http://schemas.microsoft.com/windows/2005/02/color/D65.camp"" xmlns:cam=""http://schemas.microsoft.com/windows/2005/02/color/ColorAppearanceModel"" xmlns:wcs=""http://schemas.microsoft.com/windows/2005/02/color/WcsCommonProfileTypes"" xmlns:xs=""http://www.w3.org/2001/XMLSchema-instance"">
	<cam:ProfileName>
		<wcs:Text xml:lang=""en-US"">WCS profile for sRGB viewing conditions</wcs:Text>
	</cam:ProfileName>
	<cam:Description>
		<wcs:Text xml:lang=""en-US"">Default profile for a sRGB monitor in standard viewing conditions</wcs:Text>
	</cam:Description>
	<cam:Author>
		<wcs:Text xml:lang=""en-US"">Microsoft Corporation</wcs:Text>
	</cam:Author>
	<cam:ViewingConditions>
		<cam:WhitePointName>D65</cam:WhitePointName>
		<cam:Background X=""19.0"" Y=""20.0"" Z=""21.78""/>
		<cam:Surround>Average</cam:Surround>
		<cam:LuminanceOfAdaptingField>16.0</cam:LuminanceOfAdaptingField>
		<cam:DegreeOfAdaptation>1</cam:DegreeOfAdaptation>
	</cam:ViewingConditions>
</cam:ColorAppearanceModel>";

    private static string BuildGmmpXml() =>
@"<?xml version=""1.0""?>
<gmm:GamutMapModel ID=""http://schemas.microsoft.com/windows/2005/02/color/MediaSim.gmmp"" xmlns:gmm=""http://schemas.microsoft.com/windows/2005/02/color/GamutMapModel"" xmlns:wcs=""http://schemas.microsoft.com/windows/2005/02/color/WcsCommonProfileTypes"" xmlns:xs=""http://www.w3.org/2001/XMLSchema-instance"">
	<gmm:ProfileName>
		<wcs:Text xml:lang=""en-US"">Proofing - simulate paper/media color</wcs:Text>
	</gmm:ProfileName>
	<gmm:Description>
		<wcs:Text xml:lang=""en-US"">Appropriate for ICC absolute colorimetric rendering intent workflows</wcs:Text>
	</gmm:Description>
	<gmm:Author>
		<wcs:Text xml:lang=""en-US"">Microsoft Corporation</wcs:Text>
	</gmm:Author>
	<gmm:DefaultBaselineGamutMapModel>HPMinCD_Absolute</gmm:DefaultBaselineGamutMapModel>
</gmm:GamutMapModel>";
}