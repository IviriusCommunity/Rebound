// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core.Native.Wrappers;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.CLSID;
using static TerraFX.Interop.Windows.IID;
using static TerraFX.Interop.Windows.Windows;

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
        using var launchItem = NativeString.Alloc(packageFamilyName + "!" + entryPoint);
        using var clsid = NativeValue<Guid>.Alloc(CLSID_ApplicationActivationManager);
        using var iid = NativeValue<Guid>.Alloc(IID_IApplicationActivationManager);

        using ComPtr<IApplicationActivationManager> manager = null;

        int hr = CoCreateInstance(
            clsid,
            null,
            (uint)CLSCTX.CLSCTX_INPROC_SERVER,
            iid,
            (void**)manager.GetAddressOf()
        );

        if (SUCCEEDED(hr))
        {
            uint processId;
            manager.Get()->ActivateApplication(
                launchItem.CharPointer,
                null,
                ACTIVATEOPTIONS.AO_NONE,
                &processId
            );
        }
    }
}