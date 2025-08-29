// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Security.Principal;

namespace Rebound.Core.Helpers.AppEnvironment;

public static class AppHelper
{
    public static bool IsRunningAsAdmin()
    {
        try
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    // Credit: https://github.com/HotCakeX/
    /*public static unsafe void RelaunchAsAdmin(this Application application, string package, string args)
    {
        const uint CLSCTX_LOCAL_SERVER = 0x4;

        var clsidApplicationActivationManager = new Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C");

        Windows.Win32.UI.Shell.IApplicationActivationManager activationManager;

        int hr = Windows.Win32.PInvoke.CoCreateInstance(
            clsidApplicationActivationManager,
            pUnkOuter: null,
            (Windows.Win32.System.Com.CLSCTX)CLSCTX_LOCAL_SERVER,
            out activationManager);

        if (hr < 0)
        {
            Debug.WriteLine($"CoCreateInstance failed with HRESULT: 0x{(uint)hr:X}");
            return;
        }

        activationManager.ActivateApplication(
            package.ToPCWSTR(),
            args.ToPCWSTR(),
            (Windows.Win32.UI.Shell.ACTIVATEOPTIONS)0x20000000,
            out _);
    }*/
}