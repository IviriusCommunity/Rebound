// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Core.ICC.Display;

public static class DisplayProfile
{
    /// <summary>
    /// Retrieves the path to the current color profile.
    /// </summary>
    public static unsafe string? GetCurrentProfilePath()
    {
        var handle = new HWND((void*)Process.GetCurrentProcess().MainWindowHandle);
        var hDC = GetDC(handle);
        if (hDC == nint.Zero) return null;
        try
        {
            uint size = 260;
            fixed (char* pBuffer = new char[260])
            {
                return GetICMProfileW(hDC, &size, pBuffer) != BOOL.FALSE
                    ? new string(pBuffer)
                    : null;
            }
        }
        finally { _ = ReleaseDC(handle, hDC); }
    }

    /// <summary>
    /// Retrieves calibration values for the given color profile.
    /// </summary>
    /// <param name="profilePath">
    /// The location on disk of the color profile.
    /// </param>
    public static (double gamma, double brightness, double contrast)? ReadCalibrationValues(string profilePath)
    {
        try
        {
            var bytes = File.ReadAllBytes(profilePath);
            return ReadMs10ValuesFromBytes(bytes);
        }
        catch { return null; }
    }

    private static unsafe (double gamma, double brightness, double contrast)? ReadMs10ValuesFromBytes(byte[] bytes)
    {
        if (bytes.Length < 132) return null;

        fixed (byte* data = bytes)
        {
            var tagCount = ReadU32(data + 128);
            for (var i = 0; i < tagCount; i++)
            {
                var entryBase = 132 + i * 12;
                if (entryBase + 12 > bytes.Length) break;

                var sig = ReadU32(data + entryBase);
                var offset = ReadU32(data + entryBase + 4);
                var size = ReadU32(data + entryBase + 8);

                if (sig != 0x4D533030) continue; // 'MS00'
                if (offset + size > bytes.Length) return null;

                var dmpSize = ReadU32(data + offset + 12);
                var dmpOffset = ReadU32(data + offset + 16);
                if (dmpOffset + dmpSize > size) return null;

                var startOff = (data[offset + dmpOffset] == 0xFF && data[offset + dmpOffset + 1] == 0xFE) ? 2u : 0u;
                var xml = new string((char*)(data + offset + dmpOffset + startOff), 0, (int)((dmpSize - startOff) / 2));

                var doc = System.Xml.Linq.XDocument.Parse(xml);
                var ns = "http://schemas.microsoft.com/windows/2005/02/color/ColorDeviceModel";
                var element = doc.Descendants($"{{{ns}}}GammaOffsetGainLinearGain").FirstOrDefault();
                if (element == null) return null;

                var gamma = double.Parse(element.Attribute("Gamma")!.Value, System.Globalization.CultureInfo.InvariantCulture);
                var brightness = double.Parse(element.Attribute("Offset")!.Value, System.Globalization.CultureInfo.InvariantCulture);
                var contrast = double.Parse(element.Attribute("Gain")!.Value, System.Globalization.CultureInfo.InvariantCulture);

                return (gamma, brightness, contrast);
            }
        }

        return null;
    }

    private static unsafe uint ReadU32(byte* p) => ((uint)p[0] << 24) | ((uint)p[1] << 16) | ((uint)p[2] << 8) | p[3];
}