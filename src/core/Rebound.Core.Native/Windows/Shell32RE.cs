// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Rebound.Core.Native.Windows;

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