// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Helpers;
using Rebound.Core.UI;
using TerraFX.Interop.Windows;

namespace Rebound.Forge.Cogs;

/// <summary>
/// Launches a UWP package application when applied. Ignorable.
/// </summary>
/// <remarks><see cref="IsAppliedAsync"/> will always return <see langword="true"/></remarks>
public class PackageLaunchCog : ICog
{
    private static readonly Guid CLSID_ApplicationActivationManager = new("45BA127D-10A8-46EA-8AB7-56EA9078943C");

    /// <summary>
    /// The family name of the package you want to launch. Example: Rebound.Shell_rcz2tbwv5qzb8
    /// </summary>
    public required string PackageFamilyName { get; set; }

    /// <inheritdoc/>
    public bool Ignorable { get; } = true;

    /// <summary>
    /// Creates a new instance of the <see cref="PackageLaunchCog"/> class.
    /// </summary>
    public PackageLaunchCog() { }

    /// <inheritdoc/>
    public unsafe async Task ApplyAsync()
    {
        ReboundLogger.Log($"[PackageLaunchCog] Launching package {PackageFamilyName}");

        ComPtr<IApplicationActivationManager> activationManager = default;

        HRESULT hr;
        fixed (Guid* clsid = &CLSID_ApplicationActivationManager)
        fixed (Guid* iid = &IID.IID_IApplicationActivationManager)
        {
            hr = TerraFX.Interop.Windows.Windows.CoCreateInstance(
                rclsid: clsid,
                pUnkOuter: null,
                dwClsContext: (uint)Windows.Win32.System.Com.CLSCTX.CLSCTX_LOCAL_SERVER,
                riid: iid,
                ppv: (void**)activationManager.GetAddressOf());
        }

        if (hr < 0)
        {
            ReboundLogger.Log($"[PackageLaunchCog] Failed to create ApplicationActivationManager instance. HRESULT: {hr:X8}");
            return;
        }

        UIThreadQueue.QueueAction(async () =>
        {
            hr = activationManager.Get()->ActivateApplication(
                    (PackageFamilyName + "!App").ToPointer(),
                    null,
                    ACTIVATEOPTIONS.AO_NONE,
                    null);
        });

        if (hr < 0)
        {
            ReboundLogger.Log($"[PackageLaunchCog] Failed to activate application. HRESULT: {hr:X8}");
            return;
        }
    }

    /// <inheritdoc/>
    public async Task RemoveAsync()
    {

    }

    /// <inheritdoc/>
    public async Task<bool> IsAppliedAsync()
    {
        return true;
    }
}