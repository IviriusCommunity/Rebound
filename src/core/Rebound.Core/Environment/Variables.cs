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
        Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonStartMenu),
                     "Programs", "Rebound");

    /// <summary>
    /// Represents the full path to the application's data folder within the current user's profile directory.
    /// </summary>
    /// <remarks>The data folder is located in the user's local application data directory and is named "Rebound". This path
    /// can be used to store user-specific configuration files or application data. The value is platform-dependent and
    /// resolves to the appropriate user profile location on the operating system.</remarks>
    public static readonly string ReboundDataFolder =
        Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                     "Rebound");

    /// <summary>
    /// Contains dynamically linked libraries (DLLs) for Rebound apps to use.
    /// </summary>
    public static readonly string ReboundLibsFolder =
        Path.Combine(ReboundDataFolder, "Libs");

    /// <summary>
    /// Represents the full path to the temporary log file used by the application.
    /// </summary>
    /// <remarks>The log file is located in the 'Temp' subdirectory within the application's data folder. This
    /// path can be used for reading or writing temporary log information during application execution.</remarks>
    public static readonly string ReboundLogFile =
        Path.Combine(ReboundDataFolder, "Temp", ".log");

    /// <summary>
    /// Represents the full path to the Mods folder within the application's program data directory.
    /// </summary>
    public static readonly string ReboundModsFolder =
        Path.Combine(ReboundDataFolder, "Mods");

    /// <summary>
    /// Represents the full path to the DLLs folder within Rebound's data directory.
    /// </summary>
    public static readonly string ReboundInjectedDLLsFolder =
        Path.Combine(ReboundDataFolder, "InjectedDLLs");

    /// <summary>
    /// Argument used to launch the legacy versions of Rebound apps.
    /// </summary>
    public static readonly string LegacyLaunchArgument = "legacy ";
}