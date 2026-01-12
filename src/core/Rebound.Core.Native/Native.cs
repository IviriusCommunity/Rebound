// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Rebound.Core;

public static unsafe partial class Shell32RE
{
    [LibraryImport("shell32.dll", EntryPoint = "#61")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int RunFileDlgNative(
        nint hWnd,
        nint icon,
        char* path,
        char* title,
        char* prompt,
        uint flags);

    public static HRESULT RunFileDlg(
        HWND hWnd,
        HICON icon,
        char* path,
        char* title,
        char* prompt,
        uint flags)
    {
        return (HRESULT)RunFileDlgNative(
            hWnd,
            icon,
            path,
            title,
            prompt,
            flags);
    }
}

public static class Native
{
    public static bool ArgsMatchKnownEntries(this string appName, IEnumerable<string> matches, string args)
    {
        List<string> items = [];
        foreach (var match in matches)
        {
            items.Add(match);
            items.Add($"{appName} {match}");
        }
        return items.Contains(args, StringComparer.InvariantCultureIgnoreCase);
    }

    public static unsafe HWND ToCsWin32HWND(this TerraFX.Interop.Windows.HWND hwnd)
    {
        return *(HWND*)hwnd.Value;
    }

    public static unsafe TerraFX.Interop.Windows.HWND ToTerraFXHWND(this HWND hwnd)
    {
        return *(TerraFX.Interop.Windows.HWND*)hwnd.Value;
    }

    public static unsafe Windows.Win32.Foundation.PCWSTR ToPCWSTR(this string value)
    {
        fixed (char* valueCharPtr = value)
        {
            return new Windows.Win32.Foundation.PCWSTR(valueCharPtr);
        }
    }

    public static unsafe char* ToPointer(this string value)
    {
        fixed (char* valueCharPtr = value)
        {
            return valueCharPtr;
        }
    }

    public static unsafe Windows.Win32.Foundation.PWSTR ToPWSTR(this string value)
    {
        fixed (char* valueCharPtr = value)
        {
            return new Windows.Win32.Foundation.PWSTR(valueCharPtr);
        }
    }
}