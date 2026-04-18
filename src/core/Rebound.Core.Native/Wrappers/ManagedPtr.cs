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
/// <summary>
/// Wraps a managed value in unmanaged memory, exposing a raw pointer for native interop.
/// Automatically frees the allocation when disposed.
/// </summary>
/// <typeparam name="T">The unmanaged type to wrap.</typeparam>
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

#pragma warning disable CA2225
    public static implicit operator ManagedPtr<T>(string value) => new(value);
    public static implicit operator ManagedPtr<T>(Guid value) => new(value);
    public static implicit operator T*(ManagedPtr<T> ptr) => (T*)ptr.ObjectPointer;
    public readonly char* ToCharPtr() => (char*)ObjectPointer;
    public readonly Guid* ToGuidPtr() => (Guid*)ObjectPointer;
#pragma warning restore CA2225
}

/// <summary>
/// Wraps a managed array in a contiguous unmanaged memory block, exposing a raw pointer
/// and length for native interop. Automatically frees the allocation when disposed.
/// </summary>
/// <typeparam name="T">The unmanaged element type.</typeparam>
public unsafe struct ManagedArrayPtr<T> : IEquatable<ManagedArrayPtr<T>>, IDisposable where T : unmanaged
{
    private nint _ptr;

    /// <summary>The number of elements in the allocation.</summary>
    public readonly int Length;

    public readonly void* ObjectPointer => (void*)_ptr;

    /// <summary>Total size of the allocation in bytes.</summary>
    public readonly int ByteLength => Length * sizeof(T);

    public ManagedArrayPtr(T[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        Length = values.Length;
        _ptr = Marshal.AllocHGlobal(ByteLength);
        for (var i = 0; i < values.Length; i++)
            *((T*)_ptr + i) = values[i];
    }

    /// <summary>
    /// Allocates an uninitialized block for <paramref name="length"/> elements.
    /// Useful when the native callee writes into the buffer.
    /// </summary>
    public ManagedArrayPtr(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        Length = length;
        _ptr = Marshal.AllocHGlobal(ByteLength);
    }

    /// <summary>Gets or sets the element at <paramref name="index"/>.</summary>
    /// <exception cref="IndexOutOfRangeException"/>
    public T this[int index]
    {
        readonly get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length);
            return *((T*)_ptr + index);
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length);
            *((T*)_ptr + index) = value;
        }
    }

    /// <summary>Copies the unmanaged buffer back to a new managed array.</summary>
    public readonly T[] ToArray()
    {
        var result = new T[Length];
        for (var i = 0; i < Length; i++)
            result[i] = *((T*)_ptr + i);
        return result;
    }

    /// <summary>Returns a <see cref="Span{T}"/> over the unmanaged buffer. Valid only while this instance is alive.</summary>
    public readonly Span<T> AsSpan() => new((T*)_ptr, Length);

    public void Dispose()
    {
        if (_ptr != 0)
        {
            Marshal.FreeHGlobal(_ptr);
            _ptr = 0;
        }
    }

    public readonly bool Equals(ManagedArrayPtr<T> other) => _ptr == other._ptr;
    public readonly override bool Equals(object? obj) => obj is ManagedArrayPtr<T> other && Equals(other);
    public readonly override int GetHashCode() => _ptr.GetHashCode();
    public static bool operator ==(ManagedArrayPtr<T> left, ManagedArrayPtr<T> right) => left._ptr == right._ptr;
    public static bool operator !=(ManagedArrayPtr<T> left, ManagedArrayPtr<T> right) => left._ptr != right._ptr;

#pragma warning disable CA2225
    public static implicit operator ManagedArrayPtr<T>(T[] values) => new(values);
    public static implicit operator T*(ManagedArrayPtr<T> ptr) => (T*)ptr.ObjectPointer;
#pragma warning restore CA2225
}