// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static TerraFX.Interop.Windows.Windows;

namespace Rebound.Core.Native.Wrappers;

/// <summary>
/// Encoding to marshal a <see cref="StringPtr"/> as. Choose based on what the
/// native function you're calling actually expects — this is NOT inferred automatically,
/// because guessing wrong is exactly what caused unpredictable corruption before.
/// </summary>
public enum NativeStringEncoding
{
    /// <summary>UTF-16, matches Win32 LPWSTR.</summary>
    Utf16,

    /// <summary>UTF-8, matches most POSIX / cross-platform C APIs.</summary>
    Utf8,

    /// <summary>
    /// Approximated as Latin1 (ISO-8859-1), not the true Windows system codepage — fine for
    /// Western European text, not a faithful ANSI stand-in in general. See <see cref="StringPtr.Alloc"/>.
    /// </summary>
    Ansi,
    
    BStr
}

/// <summary>
/// Owns a single native string allocation with an explicit, tracked encoding.
/// Unlike a generic pointer wrapper, this type's whole job is getting the
/// allocate/free pair to match, so mixing allocators (a common source of silent
/// heap corruption) is impossible from the outside.
/// A ref struct: cannot be stored in a field, boxed, or captured — same
/// lifetime discipline as `fixed`, so it can't outlive the call that owns it.
/// </summary>
public unsafe ref struct StringPtr
{
    private nint _ptr;

    /// <summary>
    /// The encoding this buffer was allocated with. Needed to free it correctly.
    /// </summary>
    public readonly NativeStringEncoding Encoding { get; }

    /// <summary>
    /// Checks if the underlying pointer is null.
    /// </summary>
    public readonly bool IsNull => _ptr == 0;

    private StringPtr(byte* ptr, NativeStringEncoding encoding)
    {
        _ptr = (nint)ptr;
        Encoding = encoding;
    }

    public static implicit operator StringPtr(string? value)
        => new(value, NativeStringEncoding.Utf16);

    public static StringPtr ToStringPtr(string? value)
        => new(value, NativeStringEncoding.Utf16);

    /// <summary>
    /// Allocates unmanaged memory containing <paramref name="value"/> in the requested encoding,
    /// null-terminated. Uses <see cref="NativeMemory"/> + <see cref="System.Text.Encoding"/> directly.
    /// Returns a null pointer wrapper if <paramref name="value"/> is null.
    /// </summary>
    public StringPtr(string? value, NativeStringEncoding encoding = NativeStringEncoding.Utf16)
    {
        if (value == null)
        {
            _ptr = (nint)null;
            Encoding = encoding;
            return;
        }

        if (encoding == NativeStringEncoding.BStr)
        {
            fixed (char* pValue = value)
            {
                // Allocate directly using the Windows OLE allocator
                _ptr = (nint)SysAllocStringLen(pValue, (uint)value.Length);
            }
            Encoding = encoding;
            return;
        }

        var enc = ResolveEncoding(encoding);
        int charSize = encoding == NativeStringEncoding.Utf16 ? 2 : 1;
        int byteCount = enc.GetByteCount(value);

        byte* buffer = (byte*)NativeMemory.Alloc((nuint)(byteCount + charSize));
        enc.GetBytes(value, new Span<byte>(buffer, byteCount));

        if (charSize == 1)
            buffer[byteCount] = 0;
        else
            *(char*)(buffer + byteCount) = '\0';

        _ptr = (nint)buffer;
        Encoding = encoding;
    }

    /// <summary>Reads the buffer back into a managed string, using its own recorded encoding.</summary>
    public readonly string? ToManagedString()
    {
        if (_ptr == 0)
            return null;

        if (Encoding == NativeStringEncoding.Utf16)
        {
            ReadOnlySpan<char> chars = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((char*)_ptr);
            return new string(chars);
        }
        else
        {
            ReadOnlySpan<byte> bytes = MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)_ptr);
            return ResolveEncoding(Encoding).GetString(bytes);
        }
    }

    /// <summary>Gets the raw byte pointer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* Get() => (byte*)_ptr;

    /// <summary>Gets the raw void pointer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1024 // Use properties where appropriate
    public readonly void* GetVoid() => (void*)_ptr;
#pragma warning restore CA1024 // Use properties where appropriate

    /// <summary>Gets the pointer cast to char*.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1024 // Use properties where appropriate
    public readonly char* GetChars() => (char*)_ptr;
#pragma warning restore CA1024 // Use properties where appropriate

    /// <summary>
    /// Exposes this buffer as a UTF-16 char pointer, for interop APIs that expect LPWSTR/PCWSTR. 
    /// Throws if this instance wasn't allocated as UTF-16.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly char* AsPCWSTR()
    {
        if (Encoding != NativeStringEncoding.Utf16)
            throw new InvalidOperationException($"NativeString was allocated as {Encoding}, not Utf16.");

        return (char*)_ptr;
    }

    /// <summary>
    /// Returns a pointer to the pointer (byte**). 
    /// WARNING: Do not use this to accept allocations from native functions unless 
    /// you are ABSOLUTELY SURE the native function uses the exact same C allocator as NativeMemory.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte** GetAddressOf()
    {
        return (byte**)Unsafe.AsPointer(ref _ptr);
    }

    /// <summary>
    /// Returns a pointer to the pointer (char**).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public char** GetCharAddressOf()
    {
        return (char**)Unsafe.AsPointer(ref _ptr);
    }

    /// <summary>
    /// Returns a pointer to the pointer (void**).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void** GetVoidAddressOf()
    {
        return (void**)Unsafe.AsPointer(ref _ptr);
    }

    /// <summary>
    /// Relinquishes ownership of the pointer. Returns the pointer and nulls out the struct.
    /// The caller is now responsible for freeing the memory.
    /// </summary>
    public byte* Release()
    {
        byte* temp = (byte*)_ptr;
        _ptr = 0;
        return temp;
    }

    /// <summary>
    /// Frees the current memory (if any) and optionally takes ownership of a new pointer.
    /// </summary>
    public void Reset(byte* newPtr = null)
    {
        if (_ptr != 0)
        {
            if (Encoding == NativeStringEncoding.BStr)
                SysFreeString((char*)_ptr);
            else
                NativeMemory.Free((byte*)_ptr);
        }
        _ptr = (nint)newPtr;
    }

    public void Dispose()
    {
        if (_ptr != 0)
        {
            if (Encoding == NativeStringEncoding.BStr)
                SysFreeString((char*)_ptr);
            else
                NativeMemory.Free((byte*)_ptr);
            _ptr = 0;
        }
    }

    private static Encoding ResolveEncoding(NativeStringEncoding encoding) => encoding switch
    {
        NativeStringEncoding.Utf16 => System.Text.Encoding.Unicode,
        NativeStringEncoding.Utf8 => System.Text.Encoding.UTF8,
        NativeStringEncoding.Ansi => System.Text.Encoding.Latin1,
        _ => throw new ArgumentOutOfRangeException(nameof(encoding))
    };
}

/// <summary>
/// Owns unmanaged memory for a single unmanaged value, exposing typed pointers for interop.
/// A ref struct by design: it cannot be copied into a field, boxed, captured by a lambda,
/// or held across an `await`.
/// </summary>
/// <typeparam name="T">The unmanaged type being pointed to.</typeparam>
public unsafe ref struct ObjectPtr<T> where T : unmanaged
{
    private nint _ptr;

    /// <summary>
    /// Exposes the underlying value by reference. 
    /// This allows safe reading and writing (e.g., `ptr.Value = new T();`) without raw pointer syntax.
    /// </summary>
    public readonly ref T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (_ptr == 0)
#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
#pragma warning disable CA2201 // Do not raise reserved exception types
                throw new NullReferenceException("The unmanaged pointer is null.");
#pragma warning restore CA2201 // Do not raise reserved exception types
#pragma warning restore CA1065 // Do not raise exceptions in unexpected locations
            return ref *(T*)_ptr;
        }
    }

    /// <summary>
    /// Checks if the underlying pointer is null.
    /// </summary>
    public readonly bool IsNull => _ptr == 0;

    public static implicit operator ObjectPtr<T>(T value)
        => new(value);

#pragma warning disable CA1000 // Do not declare static members on generic types
    public static ObjectPtr<T> ToObjectPtr(T value)
#pragma warning restore CA1000 // Do not declare static members on generic types
        => new(value);

    /// <summary>
    /// Allocates unmanaged memory for one <typeparamref name="T"/>, initialized to default.
    /// </summary>
    public ObjectPtr()
    {
        _ptr = (nint)NativeMemory.Alloc((nuint)sizeof(T));
        Unsafe.InitBlock((void*)_ptr, 0, (uint)sizeof(T));
    }

    /// <summary>
    /// Allocates unmanaged memory for one <typeparamref name="T"/>, initialized to <paramref name="value"/>.
    /// </summary>
    public ObjectPtr(T value = default)
    {
        _ptr = (nint)NativeMemory.Alloc((nuint)sizeof(T));
        *(T*)_ptr = value;
    }

    /// <summary>
    /// Wraps an existing unmanaged pointer, taking ownership of it.
    /// </summary>
    public ObjectPtr(T* existingPtr)
    {
        _ptr = (nint)existingPtr;
    }

    /// <summary>
    /// Gets the raw pointer (T*). Replaces the implicit operator and property.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T* Get() => (T*)_ptr;

    /// <summary>
    /// Gets the raw void pointer (void*).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1024 // Use properties where appropriate
    public readonly void* GetVoid() => (void*)_ptr;
#pragma warning restore CA1024 // Use properties where appropriate

    /// <summary>
    /// Returns a pointer to the pointer (T**). 
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable IDE0251 // Make member 'readonly'
    public T** GetAddressOf()
#pragma warning restore IDE0251 // Make member 'readonly'
    {
        return (T**)Unsafe.AsPointer(ref Unsafe.AsRef(in _ptr));
    }

    /// <summary>
    /// Returns a pointer to the pointer (void**). 
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable IDE0251 // Make member 'readonly'
    public void** GetVoidAddressOf()
#pragma warning restore IDE0251 // Make member 'readonly'
    {
        return (void**)Unsafe.AsPointer(ref Unsafe.AsRef(in _ptr));
    }

    /// <summary>
    /// Relinquishes ownership of the pointer. Returns the pointer and nulls out the struct.
    /// The caller is now responsible for freeing the memory.
    /// </summary>
    public T* Release()
    {
        T* temp = (T*)_ptr;
        _ptr = 0;
        return temp;
    }

    /// <summary>
    /// Frees the current memory (if any) and optionally takes ownership of a new pointer.
    /// </summary>
    public void Reset(T* newPtr = null)
    {
        if (_ptr != 0)
        {
            NativeMemory.Free((void*)_ptr);
        }
        _ptr = (nint)newPtr;
    }

    public void Dispose()
    {
        if (_ptr != 0)
        {
            NativeMemory.Free((void*)_ptr);
            _ptr = 0;
        }
    }
}

/// <summary>
/// Owns a contiguous unmanaged buffer of unmanaged values (a copy, not a pin — see
/// <see cref="ArrayPin{T}"/> if you want zero-copy access to an existing managed array).
/// Same ref-struct lifetime discipline as <see cref="ObjectPtr{T}"/>.
/// </summary>
/// <typeparam name="T">The unmanaged element type.</typeparam>
public unsafe ref struct ArrayPtr<T> where T : unmanaged
{
    private nint _ptr;
    private int _length;

    /// <summary>The number of elements in the buffer.</summary>
    public readonly int Length => _length;

    /// <summary>The size of the buffer in bytes.</summary>
    public readonly int ByteLength => _length * sizeof(T);

    /// <summary>Checks if the underlying pointer is null.</summary>
    public readonly bool IsNull => _ptr == 0;

    /// <summary>Allocates a copy of <paramref name="values"/> in unmanaged memory.</summary>
    public ArrayPtr(ReadOnlySpan<T> values)
    {
        _ptr = (nint)NativeMemory.Alloc((nuint)values.Length, (nuint)sizeof(T));
        values.CopyTo(new Span<T>((T*)_ptr, values.Length));
        _length = values.Length;
    }

    /// <summary>Allocates an uninitialized buffer for <paramref name="length"/> elements.</summary>
    public ArrayPtr(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        _ptr = (nint)NativeMemory.Alloc((nuint)length, (nuint)sizeof(T));
        _length = length;
    }

    /// <summary>Wraps an existing unmanaged pointer and length.</summary>
    public ArrayPtr(T* existingPtr, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        _ptr = (nint)existingPtr;
        _length = length;
    }

    /// <summary>
    /// Exposes an element by reference for reading and writing. 
    /// Returns `ref T` to allow direct mutation of fields inside unmanaged structs (e.g., `array[0].Field = 5;`).
    /// </summary>
    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _length);
            return ref ((T*)_ptr)[index];
        }
    }

    /// <summary>Gets the raw pointer (T*).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T* Get() => (T*)_ptr;

    /// <summary>Gets the raw void pointer (void*).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1024 // Use properties where appropriate
    public readonly void* GetVoid() => (void*)_ptr;
#pragma warning restore CA1024 // Use properties where appropriate

    /// <summary>Returns a pointer to the pointer (T**).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T** GetAddressOf()
    {
        return (T**)Unsafe.AsPointer(ref _ptr);
    }

    /// <summary>Returns a span view over the unmanaged buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<T> AsSpan() => new((T*)_ptr, _length);

    /// <summary>Copies the unmanaged buffer into a new managed array.</summary>
    public readonly T[] ToArray() => AsSpan().ToArray();

    /// <summary>
    /// Relinquishes ownership of the pointer. Returns the pointer and nulls out the struct.
    /// The caller is now responsible for freeing the memory.
    /// </summary>
    public T* Release()
    {
        T* temp = (T*)_ptr;
        _ptr = 0;
        _length = 0;
        return temp;
    }

    /// <summary>
    /// Frees the current memory (if any) and optionally takes ownership of a new pointer and length.
    /// </summary>
    public void Reset(T* newPtr = null, int newLength = 0)
    {
        if (_ptr != 0)
        {
            NativeMemory.Free((void*)_ptr);
        }
        _ptr = (nint)newPtr;
        _length = newLength;
    }

    public void Dispose()
    {
        if (_ptr != 0)
        {
            NativeMemory.Free((void*)_ptr);
            _ptr = 0;
            _length = 0;
        }
    }
}

/// <summary>
/// Pins an EXISTING managed array in place (no copy) so native code can read/write it
/// directly — the true equivalent of `fixed (T* p = array)`, but with a disposable
/// lifetime instead of a lexical block. Not a ref struct: sometimes you legitimately
/// need to hold a pin across a field (e.g. a long-lived native callback buffer), and
/// GCHandle itself is safe to store — just be disciplined about calling Dispose.
/// </summary>
/// <typeparam name="T">The unmanaged element type of the pinned array.</typeparam>
public unsafe struct ArrayPin<T> : IDisposable, IEquatable<ArrayPin<T>> where T : unmanaged
{
    private GCHandle _handle;

    /// <summary>Checks if the handle is allocated and active.</summary>
    public readonly bool IsAllocated => _handle.IsAllocated;

    /// <summary>Pins <paramref name="array"/> in place. The array is NOT copied; writes through pointers affect it directly.</summary>
    public ArrayPin(T[] array)
    {
        ArgumentNullException.ThrowIfNull(array);
        _handle = GCHandle.Alloc(array, GCHandleType.Pinned);
    }

    /// <summary>Gets the raw pointer (T*) to the pinned array.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T* Get() => _handle.IsAllocated ? (T*)_handle.AddrOfPinnedObject() : null;

    /// <summary>Gets the raw void pointer (void*) to the pinned array.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void* GetVoid() => _handle.IsAllocated ? (void*)_handle.AddrOfPinnedObject() : null;

    public void Dispose()
    {
        if (_handle.IsAllocated)
        {
            _handle.Free();
            _handle = default;
        }
    }

    public readonly bool Equals(ArrayPin<T> other) => _handle.Equals(other._handle);
    public readonly override bool Equals(object? obj) => obj is ArrayPin<T> other && Equals(other);
    public readonly override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(ArrayPin<T> left, ArrayPin<T> right) => left.Equals(right);
    public static bool operator !=(ArrayPin<T> left, ArrayPin<T> right) => !left.Equals(right);
}

/// <summary>
/// Passes an arbitrary managed object through native code as an opaque pointer
/// (the classic "user data" / callback-context pattern) without pinning or copying it —
/// the object itself is free to move on the GC heap; only the handle is stable.
/// </summary>
#pragma warning disable CA1720 // Identifier contains type name
public unsafe struct ObjectHandle : IDisposable, IEquatable<ObjectHandle>
{
    private GCHandle _handle;

    /// <summary>Checks if the handle is allocated and active.</summary>
    public readonly bool IsAllocated => _handle.IsAllocated;

    /// <summary>Opaque pointer to pass into native code as the callback context / user-data parameter.</summary>
    public readonly nint Pointer => _handle.IsAllocated ? GCHandle.ToIntPtr(_handle) : 0;

    public ObjectHandle(object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _handle = GCHandle.Alloc(value);
    }

    /// <summary>Gets the opaque pointer as a void* for native interop.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1024 // Use properties where appropriate
    public readonly void* GetVoid() => (void*)Pointer;
#pragma warning restore CA1024 // Use properties where appropriate

    /// <summary>Recovers the original managed object from the pointer handed back by native code.</summary>
    public static object FromPointer(nint pointer)
    {
        if (pointer == 0)
            throw new ArgumentException("The pointer cannot be zero.", nameof(pointer));

        return GCHandle.FromIntPtr(pointer).Target!;
    }

    /// <summary>Recovers the original managed object from a void* pointer.</summary>
    public static object FromPointer(void* pointer) => FromPointer((nint)pointer);

    /// <summary>Typed convenience overload of <see cref="FromPointer(nint)"/>.</summary>
    public static T FromPointer<T>(nint pointer) where T : class => (T)FromPointer(pointer);

    /// <summary>Typed convenience overload of <see cref="FromPointer(void*)"/>.</summary>
    public static T FromPointer<T>(void* pointer) where T : class => (T)FromPointer((nint)pointer);

    public void Dispose()
    {
        if (_handle.IsAllocated)
        {
            _handle.Free();
            _handle = default; // Prevents double-free and clears state
        }
    }

    public readonly bool Equals(ObjectHandle other) => _handle.Equals(other._handle);
    public readonly override bool Equals(object? obj) => obj is ObjectHandle other && Equals(other);
    public readonly override int GetHashCode() => _handle.GetHashCode();

    public static bool operator ==(ObjectHandle left, ObjectHandle right) => left.Equals(right);
    public static bool operator !=(ObjectHandle left, ObjectHandle right) => !left.Equals(right);
}
#pragma warning restore CA1720 // Identifier contains type name