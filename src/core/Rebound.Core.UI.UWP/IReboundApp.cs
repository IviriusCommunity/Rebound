// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.IPC;

namespace Rebound.Core.UI.UWP;

/// <summary>
/// Interface for a Rebound app that supports launching its legacy equivalent.
/// </summary>
public interface IReboundLegacySupportApp
{
    /// <summary>
    /// The name of the legacy executable to be launched (winver.exe, msinfo32.exe, etc.); it must contain the file extension.
    /// </summary>
    string LegacyExecutableName { get; }

    /// <summary>
    /// Launches the legacy equivalent of the current application.
    /// </summary>
    /// <param name="args">
    /// The arguments to launch the legacy executable with.
    /// </param>
    void LaunchLegacy(string args);
}

/// <summary>
/// Interface for a Rebound app that supports connecting to Rebound Service Host.
/// </summary>
public interface IReboundPipeClientApp
{
    /// <summary>
    /// The pipe client class responsible for communicating with Rebound Service Host.
    /// </summary>
    PipeClient? ReboundPipeClient { get; }

    /// <summary>
    /// In case Rebound Service Host fails to launch by various reasons, call this function.
    /// </summary>
    void RunServiceHostFailedToLaunchFallback();
}