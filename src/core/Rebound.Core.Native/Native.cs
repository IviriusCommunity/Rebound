// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Rebound.Core;

/// <summary>
/// Wraps a managed value in unmanaged memory, exposing a raw pointer for native interop. Automatically frees the allocation when disposed.
/// </summary>
/// <typeparam name="T">
/// The type of the pointer used.
/// </typeparam>
public unsafe struct ManagedPtr<T> : IEquatable<ManagedPtr<T>>, IDisposable where T : unmanaged
{
    private nint _ptr;
    public readonly void* ObjectPointer => (void*)_ptr;

    public ManagedPtr(T value)
    {
        _ptr = Marshal.AllocHGlobal(sizeof(T));
        *(T*)_ptr = value;
    }

    public ManagedPtr(Guid value)
    {
        _ptr = Marshal.AllocHGlobal(sizeof(T));
        *(Guid*)_ptr = value;
    }

    public ManagedPtr(string value)
    {
        _ptr = Marshal.StringToHGlobalUni(value);
    }

    public void Dispose()
    {
        if (_ptr != 0)
        {
            Marshal.FreeHGlobal(_ptr);
            _ptr = 0;
        }
    }

    public readonly bool Equals(ManagedPtr<T> other) => _ptr == other._ptr;
    public readonly override bool Equals(object? obj) => obj is ManagedPtr<T> other && Equals(other);
    public readonly override int GetHashCode() => _ptr.GetHashCode();
    public static bool operator ==(ManagedPtr<T> left, ManagedPtr<T> right) => left._ptr == right._ptr;
    public static bool operator !=(ManagedPtr<T> left, ManagedPtr<T> right) => left._ptr != right._ptr;

#pragma warning disable CA2225 // Operator overloads have named alternates
    public static implicit operator ManagedPtr<T>(string value) => new(value);

    public static implicit operator ManagedPtr<T>(Guid value) => new(value);

    public static implicit operator char*(ManagedPtr<T> pinned) => (char*)pinned.ObjectPointer;
    public readonly char* ToCharPtr() => (char*)ObjectPointer;

    public static implicit operator Guid*(ManagedPtr<T> pinned) => (Guid*)pinned.ObjectPointer;
    public readonly Guid* ToGuidPtr() => (Guid*)ObjectPointer;
#pragma warning restore CA2225 // Operator overloads have named alternates
}

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

public static class NativeMethods
{
    public static ulong FileTimeToUlong(FILETIME ft)
        => ((ulong)ft.dwHighDateTime << 32) | ft.dwLowDateTime;

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

    [Obsolete("Dangerous code.")]
    public static unsafe Windows.Win32.Foundation.PCWSTR ToPCWSTR(this string value)
    {
        fixed (char* valueCharPtr = value)
        {
            return new Windows.Win32.Foundation.PCWSTR(valueCharPtr);
        }
    }

    [Obsolete("Dangerous code.")]
    public static unsafe char* ToPointer(this string value)
    {
        fixed (char* valueCharPtr = value)
        {
            return valueCharPtr;
        }
    }
}