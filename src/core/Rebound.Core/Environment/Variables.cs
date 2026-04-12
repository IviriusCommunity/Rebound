// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Core;

/// <summary>
/// Contains variables that are used everywhere in Rebound: paths, versions, etc.
/// </summary>
public static partial class Variables
{
    /// <summary>
    /// Represents the full file system path to the application's Start Menu folder for all users.
    /// </summary>
    /// <remarks>This path is constructed using the common Start Menu location and includes the 'Programs' and
    /// 'Rebound' subfolders. It can be used to store shortcuts or other application-related files that should appear in
    /// the Start Menu for every user on the system.</remarks>
    public static readonly string ReboundStartMenuFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                     "Programs", "Rebound");

    /// <summary>
    /// Represents the full path to the application's data folder within the current user's profile directory.
    /// </summary>
    /// <remarks>The data folder is located in the user's local application data directory and is named "Rebound". This path
    /// can be used to store user-specific configuration files or application data. The value is platform-dependent and
    /// resolves to the appropriate user profile location on the operating system.</remarks>
    public static readonly string ReboundDataFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "Rebound");

    /// <summary>
    /// Represents the full path to the temporary log file used by the application.
    /// </summary>
    /// <remarks>The log file is located in the 'Temp' subdirectory within the application's data folder. This
    /// path can be used for reading or writing temporary log information during application execution.</remarks>
    public static readonly string ReboundLogFile =
        Path.Combine(ReboundDataFolder, "Temp", ".log");

    /// <summary>
    /// Represents the full path to the application's shared program data folder under the system's Common Program Files
    /// directory.
    /// </summary>
    /// <remarks>This path is intended for storing data that is accessible to all users of the system and modding components that
    /// are applied for all users. The folder is created by combining the Common Program Files directory with the "Rebound" subfolder. 
    /// Ensure that the application has appropriate permissions to read from and write to this location.</remarks>
    public static readonly string ReboundProgramFilesFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                     "Rebound");

    /// <summary>
    /// Rebound program data folder for storing common data accessible for every user.
    /// </summary>
    /// <remarks>
    /// To be used for storing binaries, mods, and other data requested by system level changes.
    /// </remarks>
    public static readonly string ReboundSharedDataFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Rebound");

    /// <summary>
    /// Represents the full path to the Mods folder within the application's program data directory.
    /// </summary>
    public static readonly string ReboundModsFolder =
        Path.Combine(ReboundSharedDataFolder, "Mods");

    /// <summary>
    /// Represents the full path to the DLLs folder within the application's program data directory.
    /// </summary>
    public static readonly string ReboundInjectedDLLsFolder =
        Path.Combine(ReboundSharedDataFolder, "DLLs");

    /// <summary>
    /// Represents the full file system path to the Rebound Launcher executable.
    /// </summary>
    public static readonly string ReboundLauncherPath =
        Path.Combine(ReboundSharedDataFolder, "Rebound.Launcher.exe");

    /// <summary>
    /// Gets the full file system path to the Rebound Uninstaller executable.
    /// </summary>
    /// <remarks>This path is constructed by combining the Rebound uninstaller folder with the executable file
    /// name. Use this value when launching or referencing the Rebound Uninstaller from code.</remarks>
    public static readonly string ReboundUninstallerPath =
        Path.Combine(ReboundProgramFilesFolder, "Rebound.Uninstaller.exe");

    /// <summary>
    /// File containing information about the currently installed version of Rebound.
    /// </summary>
    public static readonly string ReboundCurrentVersionPath =
        Path.Combine(ReboundDataFolder, "version.txt");

    /// <summary>
    /// Argument used to launch the legacy versions of Rebound apps.
    /// </summary>
    public static readonly string LegacyLaunchArgument = "legacy ";
}