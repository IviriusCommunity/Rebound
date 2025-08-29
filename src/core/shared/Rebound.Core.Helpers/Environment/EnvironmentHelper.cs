// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Core.Helpers.Environment;

public static class WindowsEnvironment
{
    public static string GetWindowsInstallationDrivePath()
    {
        // Get the system directory path
        var systemPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows);

        // Extract the drive letter
        var driveLetter = System.IO.Path.GetPathRoot(systemPath);
        return driveLetter ??= "C:\\";
    }
}