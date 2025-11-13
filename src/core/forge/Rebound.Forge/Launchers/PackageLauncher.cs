// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Rebound.Core;
using Rebound.Core.Helpers;
using Rebound.Core.UI;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Rebound.Forge.Launchers;

/// <summary>
/// Launcher class used to launch a package.
/// </summary>
public class PackageLauncher : ILauncher
{
    private static readonly Guid CLSID_ApplicationActivationManager = new("45BA127D-10A8-46EA-8AB7-56EA9078943C");

    /// <summary>
    /// The package family name.
    /// </summary>
    public required string PackageFamilyName { get; set; }

    [DllImport("Wtsapi32.dll", SetLastError = true)]
    static unsafe extern bool WTSQueryUserToken(ulong sessionId, void* Token);

    public async unsafe Task LaunchAsync()
    {
        uint sessionId = TerraFX.Interop.Windows.Windows.WTSGetActiveConsoleSessionId();
        if (sessionId == uint.MaxValue)
            throw new InvalidOperationException("No active session");

        // Get user token for the active session
        HANDLE userToken = new();
        bool gotToken = WTSQueryUserToken(sessionId, &userToken);
        if (!gotToken)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

        // Prepare startup info
        STARTUPINFOW startupInfo = new();
        startupInfo.cb = (uint)sizeof(STARTUPINFOW);

        PROCESS_INFORMATION processInfo;

        bool result = TerraFX.Interop.Windows.Windows.CreateProcessAsUserW(
            userToken,
            Variables.ReboundLauncherPath.ToPointer(),
            $"\"{Variables.ReboundLauncherPath}\" --launchPackage {PackageFamilyName}!App".ToPointer(),
            null,
            null,
            false,
            0x00000400 | 0x00000010,
            null,
            null,
            &startupInfo,
            &processInfo);

        if (!result)
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());

        // Close handles
        TerraFX.Interop.Windows.Windows.CloseHandle(userToken);
        TerraFX.Interop.Windows.Windows.CloseHandle(processInfo.hProcess);
        TerraFX.Interop.Windows.Windows.CloseHandle(processInfo.hThread);
        return;

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
}