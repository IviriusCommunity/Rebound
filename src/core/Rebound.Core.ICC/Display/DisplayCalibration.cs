// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.ICC.Curves;
using Rebound.Core.ICC.Profiles;

namespace Rebound.Core.ICC.Display;

public static class DisplayCalibration
{
    /*public static void Calibrate(double gamma, double brightness, double contrast, string displayDevice = @"\\.\DISPLAY1")
    {
        var bytes = WcsProfile.Generate(gamma, brightness, contrast);
        var path = Path.GetTempFileName() + ".icm";
        File.WriteAllBytes(path, bytes);
        DisplayProfile.InstallAndApply(path, displayDevice);
    }*/

    public static void Reset(string displayDevice = @"\\.\DISPLAY1")
    {
        // WcsGetDefaultColorProfile to find the system default
        // ColorProfileSetDisplayDefaultAssociation back to it
        DisplayProfile.RestoreDefault(displayDevice);
    }
}