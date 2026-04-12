// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Native.Wrappers;
using TerraFX.Interop.Windows;
using WinRT;

namespace Rebound.Forge.Engines;

/// <summary>
/// Contains helper methods to launch application packages
/// </summary>
public static class ApplicationLaunchEngine
{
    /// <summary>
    /// Launches an application package.
    /// </summary>
    /// <param name="packageFamilyName">
    /// The package family name (ex: Rebound.Hub_rcz2tbwv5qzb8)
    /// </param>
    /// <param name="entryPoint">
    /// The package entry point (ex: App)
    /// </param>
    public static unsafe void LaunchApp(string packageFamilyName, string entryPoint = "App")
    {
        using var launchItem = new ManagedPtr<char>(packageFamilyName + "!" + entryPoint);
        var manager = new ApplicationActivationManager();
        manager.As<IApplicationActivationManager>().ActivateApplication(launchItem, null, ACTIVATEOPTIONS.AO_NONE, null);
    }
}