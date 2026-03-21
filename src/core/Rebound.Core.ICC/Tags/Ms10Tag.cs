// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Text;

namespace Rebound.Core.ICC.Tags;

/// <summary>
/// Proprietary XML tag for ICC profiles used in Windows color profiles.
/// </summary>
public static class Ms10Tag
{
    public static byte[] Build(
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

    private static string EscapeXml(string s) => s
        .Replace("&", "&amp;", StringComparison.InvariantCultureIgnoreCase)
        .Replace("<", "&lt;", StringComparison.InvariantCultureIgnoreCase)
        .Replace(">", "&gt;", StringComparison.InvariantCultureIgnoreCase)
        .Replace("\"", "&quot;", StringComparison.InvariantCultureIgnoreCase);

    private static string BuildDmpXml(
    string title, string description,
    double gamma, double brightness, double contrast, DateTime now)
    {
        // WCS uses inverse gamma (1/gamma) in ParameterizedCurves
        var invGamma = 1.0 / gamma;
        var timestamp = now.ToString("yyyy-MM-ddTHH:mm:ss", null);

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