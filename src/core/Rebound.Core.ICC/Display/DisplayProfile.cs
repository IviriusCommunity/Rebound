using Rebound.Core.ICC.Curves;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Core.ICC.Display;

public static class DisplayProfile
{
    // ── existing, unchanged ────────────────────────────────────────────────

    public static unsafe bool ApplyProfile(string profilePath)
    {
        using ManagedPtr<char> pProfilePath = profilePath;
        var handle = new HWND((void*)Process.GetCurrentProcess().MainWindowHandle);
        var hDC = GetDC(handle);
        if (hDC == nint.Zero) return false;
        try { return SetICMProfileW(hDC, pProfilePath); }
        finally { _ = ReleaseDC(handle, hDC); }
    }

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

    // ── new ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads the vcgt calibration curves from a profile using the WCS API.
    /// </summary>
    public static unsafe CurveChannel? ReadCalibration(string profilePath)
    {
        var pData = Marshal.StringToHGlobalUni(profilePath);
        try
        {
            var prof = new PROFILE
            {
                dwType = 1, // PROFILE_FILENAME
                pProfileData = (void*)pData,
                cbDataSize = (uint)((profilePath.Length + 1) * 2)
            };

            var hProfile = OpenColorProfileW(
                &prof,
                0x1,         // PROFILE_READ
                0x1,         // FILE_SHARE_READ
                3);          // OPEN_EXISTING

            if (hProfile == IntPtr.Zero) return null;

            try { return ReadVcgt(hProfile); }
            finally { CloseColorProfile(hProfile); }
        }
        finally { Marshal.FreeHGlobal(pData); }
    }

    /// <summary>
    /// Installs a profile into the Windows COLOR directory and associates
    /// it with the specified display as the user-scope default.
    /// </summary>
    public static unsafe bool InstallAndApply(string profilePath, string displayDevice)
    {
        fixed (char* pPath = profilePath)
        {
            if (!InstallColorProfileW(null, pPath)) return false;
        }

        var fileName = Path.GetFileName(profilePath);
        var luid = GetDisplayLuid(displayDevice);

        fixed (char* pFileName = fileName)
        {
            ColorProfileAddDisplayAssociation(
                WCS_PROFILE_MANAGEMENT_SCOPE.WCS_PROFILE_MANAGEMENT_SCOPE_CURRENT_USER,
                pFileName,
                luid,
                (uint)COLORPROFILETYPE.CPT_ICC,
                new((int)COLORPROFILESUBTYPE.CPST_NONE),
                BOOL.TRUE);

            ColorProfileSetDisplayDefaultAssociation(
                WCS_PROFILE_MANAGEMENT_SCOPE.WCS_PROFILE_MANAGEMENT_SCOPE_CURRENT_USER,
                pFileName,
                COLORPROFILETYPE.CPT_ICC,
                COLORPROFILESUBTYPE.CPST_NONE,
                luid,
                0); // sourceID: 0 = first source on adapter
            var colorDir = GetColorDirectory();
            if (colorDir != null)
            {
                var fullPath = Path.Combine(colorDir, fileName);
                ApplyProfile(fullPath);
            }

            return true;
        }

        return true;
    }

    private static unsafe LUID GetDisplayLuid(string deviceName)
    {
        // QueryDisplayConfig to find the LUID for a given GDI device name
        uint pathCount = 0, modeCount = 0;

        // First call to get buffer sizes
        QueryDisplayConfig(
            QDC_ONLY_ACTIVE_PATHS,
            &pathCount, null,
            &modeCount, null,
            null);

        var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

        fixed (DISPLAYCONFIG_PATH_INFO* pPaths = paths)
        fixed (DISPLAYCONFIG_MODE_INFO* pModes = modes)
        {
            QueryDisplayConfig(
                QDC_ONLY_ACTIVE_PATHS,
                &pathCount, pPaths,
                &modeCount, pModes,
                null);

            for (var i = 0; i < pathCount; i++)
            {
                var info = new DISPLAYCONFIG_SOURCE_DEVICE_NAME
                {
                    header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
                    {
                        type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME,
                        size = (uint)sizeof(DISPLAYCONFIG_SOURCE_DEVICE_NAME),
                        adapterId = paths[i].sourceInfo.adapterId,
                        id = paths[i].sourceInfo.id
                    }
                };

                if (DisplayConfigGetDeviceInfo(&info.header) != 0) continue;

                var gdiName = new string(info.viewGdiDeviceName);
                if (gdiName.TrimEnd('\0').Equals(deviceName, StringComparison.OrdinalIgnoreCase))
                    return paths[i].sourceInfo.adapterId;
            }
        }

        // Fallback: return zeroed LUID, WCS will use the primary display
        return default;
    }

    /// <summary>
    /// Restores the system default color profile for the specified display.
    /// </summary>
    public static unsafe void RestoreDefault(string displayDevice)
    {
        var luid = GetDisplayLuid(displayDevice);
        uint size = 0;

        WcsGetDefaultColorProfileSize(
            WCS_PROFILE_MANAGEMENT_SCOPE.WCS_PROFILE_MANAGEMENT_SCOPE_SYSTEM_WIDE,
            null,   // device name param is gone in newer signature
            COLORPROFILETYPE.CPT_ICC,
            COLORPROFILESUBTYPE.CPST_NONE,
            0,
            &size);

        if (size == 0) return;

        fixed (char* pBuffer = new char[size])
        {
            if (!WcsGetDefaultColorProfile(
                WCS_PROFILE_MANAGEMENT_SCOPE.WCS_PROFILE_MANAGEMENT_SCOPE_SYSTEM_WIDE,
                null,
                COLORPROFILETYPE.CPT_ICC,
                COLORPROFILESUBTYPE.CPST_NONE,
                0,
                size,
                pBuffer)) return;

            var defaultName = new string(pBuffer).TrimEnd('\0');
            var colorDir = GetColorDirectory();
            if (colorDir == null) return;

            ApplyProfile(Path.Combine(colorDir, defaultName));
        }
    }

    // ── private helpers ────────────────────────────────────────────────────

    private static unsafe string? GetColorDirectory()
    {
        uint size = 0;
        GetColorDirectoryW(null, null, &size);
        if (size == 0) return null;

        fixed (char* pBuffer = new char[size])
        {
            return GetColorDirectoryW(null, pBuffer, &size)
                ? new string(pBuffer)
                : null;
        }
    }

    private static unsafe CurveChannel? ReadVcgt(HPROFILE hProfile)
    {
        const uint VCGT = 0x76636774;

        uint size = 0;
        BOOL reference = BOOL.FALSE;
        if (!GetColorProfileElement(hProfile, VCGT, 0, &size, null, &reference))
            return null;

        var buffer = new byte[size];
        fixed (byte* pBuffer = buffer)
        {
            if (!GetColorProfileElement(hProfile, VCGT, 0, &size, pBuffer, &reference))
                return null;

            return ParseVcgt(pBuffer, size);
        }
    }

    private static unsafe CurveChannel? ParseVcgt(byte* d, uint size)
    {
        if (size < 18) return null;

        var gammaType = ReadU32(d + 8);

        if (gammaType == 0) // table
        {
            var channels = ReadU16(d + 12);
            var count = ReadU16(d + 14);
            var entrySize = ReadU16(d + 16);

            if (channels != 3 || count == 0) return null;

            var stride = (uint)(count * entrySize);
            var red = ReadChannel(d + 18, count, entrySize);
            var green = ReadChannel(d + 18 + stride, count, entrySize);
            var blue = ReadChannel(d + 18 + stride * 2, count, entrySize);

            return new CurveChannel(
                Resample(red, count),
                Resample(green, count),
                Resample(blue, count));
        }
        else if (gammaType == 1) // formula
        {
            // Each channel: gamma (u8.8), min (u8.8), max (u8.8)
            var rg = ReadU16(d + 12) / 256.0;
            var gg = ReadU16(d + 18) / 256.0;
            var bg = ReadU16(d + 24) / 256.0;

            return new CurveChannel(
                new GammaCurve(rg),
                new GammaCurve(gg),
                new GammaCurve(bg));
        }

        return null;
    }

    private static unsafe ushort[] ReadChannel(byte* src, ushort count, ushort entrySize)
    {
        var result = new ushort[count];
        for (var i = 0; i < count; i++)
            result[i] = entrySize == 2
                ? ReadU16(src + i * 2)
                : (ushort)(src[i] * 257); // 8-bit → 16-bit
        return result;
    }

    private static GammaCurve Resample(ushort[] src, ushort srcCount)
    {
        if (srcCount == GammaCurve.EntryCount) return new GammaCurve(src);

        var dst = new ushort[GammaCurve.EntryCount];
        for (var i = 0; i < GammaCurve.EntryCount; i++)
        {
            var t = i * (srcCount - 1.0) / (GammaCurve.EntryCount - 1.0);
            var lo = (int)t;
            var hi = Math.Min(lo + 1, srcCount - 1);
            var frac = t - lo;
            dst[i] = (ushort)(src[lo] * (1 - frac) + src[hi] * frac);
        }
        return new GammaCurve(dst);
    }

    private static unsafe uint ReadU32(byte* p) => ((uint)p[0] << 24) | ((uint)p[1] << 16) | ((uint)p[2] << 8) | p[3];
    private static unsafe ushort ReadU16(byte* p) => (ushort)((p[0] << 8) | p[1]);
}