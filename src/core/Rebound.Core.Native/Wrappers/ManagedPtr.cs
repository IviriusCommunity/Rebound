// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Rebound.Core.Native.Wrappers;

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

    public ManagedPtr(T[] values)
    {
        _ptr = Marshal.AllocHGlobal((int)(sizeof(T) * values?.Length)!);
        for (var i = 0; i < values?.Length; i++)
            *((T*)_ptr + i) = values[i];
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

    public static implicit operator ManagedPtr<T>(T[] values) => new(values);

    public static implicit operator T*(ManagedPtr<T> pinned) => (T*)pinned.ObjectPointer;

    public readonly char* ToCharPtr() => (char*)ObjectPointer;

    public readonly Guid* ToGuidPtr() => (Guid*)ObjectPointer;
#pragma warning restore CA2225 // Operator overloads have named alternates
}