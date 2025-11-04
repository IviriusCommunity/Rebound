// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Helpers;
using TerraFX.Interop.Windows;

namespace Rebound.Forge.Cogs;

/// <summary>
/// Launches a UWP package application when applied. Does nothing when removed.
/// </summary>
/// <remarks><see cref="IsAppliedAsync"/> will always return <see langword="true"/></remarks>
public class PackageLaunchCog : ICog
{
    private static readonly Guid CLSID_ApplicationActivationManager = new("45BA127D-10A8-46EA-8AB7-56EA9078943C");

    public required string PackageFamilyName { get; set; }

    public bool Ignorable { get; } = true;

    public PackageLaunchCog() { }

    public unsafe async Task ApplyAsync()
    {
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

        activationManager.Get()->ActivateApplication(
            PackageFamilyName.ToPointer(),
            null,
            ACTIVATEOPTIONS.AO_NONE,
            null);
    }

    public async Task RemoveAsync()
    {

    }

    public async Task<bool> IsAppliedAsync()
    {
        return true;
    }
}