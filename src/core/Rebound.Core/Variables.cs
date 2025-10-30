// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Core;

/// <summary>
/// Contains variables that are used everywhere in Rebound: paths, versions, etc.
/// </summary>
public static class Variables
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
    /// <remarks>The data folder is located in the user's home directory and is named ".rebound". This path
    /// can be used to store user-specific configuration files or application data. The value is platform-dependent and
    /// resolves to the appropriate user profile location on the operating system.</remarks>
    public static readonly string ReboundDataFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                     ".rebound");

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
    public static readonly string ReboundProgramDataFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
                     "Rebound");

    /// <summary>
    /// Represents the full path to the Mods folder within the application's program data directory.
    /// </summary>
    public static readonly string ReboundProgramDataModsFolder =
        Path.Combine(ReboundProgramDataFolder, "Mods");

    /// <summary>
    /// Represents the full file system path to the Rebound Launcher executable.
    /// </summary>
    public static readonly string ReboundLauncherPath =
        Path.Combine(ReboundProgramDataFolder, "Rebound.Launcher.exe");

    /// <summary>
    /// Represents the current version identifier for Rebound as a whole.
    /// </summary>
    public const string ReboundVersion = "v0.0.10.1 Developer Preview";
}