// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace Rebound.Core.Native.Wrappers;

/// <summary>
/// Encoding to marshal a <see cref="NativeString"/> as. Choose based on what the
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
    /// Western European text, not a faithful ANSI stand-in in general. See <see cref="NativeString.Alloc"/>.
    /// </summary>
    Ansi
}

/// <summary>
/// Owns a single native string allocation with an explicit, tracked encoding.
/// Unlike a generic pointer wrapper, this type's whole job is getting the
/// allocate/free pair to match, so mixing allocators (a common source of silent
/// heap corruption) is impossible from the outside.
/// A ref struct: cannot be stored in a field, boxed, or captured — same
/// lifetime discipline as `fixed`, so it can't outlive the call that owns it.
/// </summary>
public unsafe ref struct NativeString
{
    private nint _ptr;

    /// <summary>The encoding this buffer was allocated with. Needed to free it correctly.</summary>
    public readonly NativeStringEncoding Encoding { get; }

    /// <summary>Raw pointer to pass to native code. Null if allocated from a null string.</summary>
    public readonly void* Pointer => (void*)_ptr;

    /// <summary>Raw character pointer to pass to native code. Null if allocated from a null string.</summary>
    public readonly char* CharPointer => (char*)_ptr;

    private NativeString(nint ptr, NativeStringEncoding encoding)
    {
        _ptr = ptr;
        Encoding = encoding;
    }

    /// <summary>
    /// Allocates unmanaged memory containing <paramref name="value"/> in the requested encoding,
    /// null-terminated. Uses <see cref="NativeMemory"/> + <see cref="System.Text.Encoding"/> directly
    /// rather than Marshal's String* helpers.
    /// NOTE: <see cref="NativeStringEncoding.Ansi"/> is approximated with Latin1 (ISO-8859-1) here,
    /// since .NET has no portable Encoding for the Windows system codepage without either
    /// Marshal.StringToHGlobalAnsi or registering CodePagesEncodingProvider. That's correct for
    /// Western European text but NOT a faithful stand-in for arbitrary ANSI codepages — if you need
    /// exact Windows ANSI semantics, that's a real reason to keep Marshal for this one case.
    /// </summary>
    public static NativeString Alloc(string? value, NativeStringEncoding encoding = NativeStringEncoding.Utf16)
    {
        value ??= string.Empty;
        var enc = ResolveEncoding(encoding);
        var charSize = encoding == NativeStringEncoding.Utf16 ? 2 : 1;

        var byteCount = enc.GetByteCount(value);
        var buffer = (byte*)NativeMemory.Alloc((nuint)(byteCount + charSize)); // + null terminator

        enc.GetBytes(value, new Span<byte>(buffer, byteCount));
        new Span<byte>(buffer + byteCount, charSize).Clear();

        return new NativeString((nint)buffer, encoding);
    }

    /// <summary>Reads the buffer back into a managed string, using its own recorded encoding.</summary>
    public readonly string ToManagedString()
    {
        if (_ptr == 0)
            return string.Empty;

        var enc = ResolveEncoding(Encoding);
        var bytes = (byte*)_ptr;
        var length = 0;

        if (Encoding == NativeStringEncoding.Utf16)
        {
            var chars = (char*)_ptr;
            while (chars[length] != '\0') length++;
            return enc.GetString((byte*)chars, length * 2);
        }

        while (bytes[length] != 0) length++;
        return enc.GetString(bytes, length);
    }

    private static System.Text.Encoding ResolveEncoding(NativeStringEncoding encoding) => encoding switch
    {
        NativeStringEncoding.Utf16 => System.Text.Encoding.Unicode,
        NativeStringEncoding.Utf8 => System.Text.Encoding.UTF8,
        NativeStringEncoding.Ansi => System.Text.Encoding.Latin1,
        _ => throw new ArgumentOutOfRangeException(nameof(encoding))
    };

    // Pattern-based dispose — works with `using` on a ref struct without requiring
    // the type to implement IDisposable (which needs C# 13+ for ref structs).
    /// <summary>
    /// Exposes this buffer as a UTF-16 char pointer, for interop APIs that expect
    /// LPWSTR/PCWSTR. Throws if this instance wasn't allocated as UTF-16 — a mismatched
    /// encoding here is exactly the kind of bug this type exists to catch at the source
    /// instead of producing "unpredictable" native-side corruption.
    /// </summary>
    public readonly char* AsPCWSTR()
    {
        if (Encoding != NativeStringEncoding.Utf16)
            throw new InvalidOperationException($"NativeString was allocated as {Encoding}, not Utf16.");

        return (char*)_ptr;
    }

    public void Dispose()
    {
        if (_ptr == 0)
            return;

        // Single allocator now (NativeMemory) for every encoding — no more
        // matching a free function to how the buffer was allocated.
        NativeMemory.Free((void*)_ptr);
        _ptr = 0;
    }
}

/// <summary>
/// Owns unmanaged memory for a single unmanaged value, exposing a typed pointer for interop.
/// A ref struct by design: it cannot be copied into a field, boxed, captured by a lambda,
/// or held across an `await`, which is what previously allowed two structs to alias the
/// same allocation and led to double-free / use-after-free. If you need a heap-storable
/// version, see <see cref="PinnedHandle{T}"/> instead — different tool, different guarantees.
/// </summary>
/// <typeparam name="T">The unmanaged type being pointed to.</typeparam>
public unsafe ref struct NativeValue<T> where T : unmanaged
{
    private nint _ptr;

    public readonly T* Pointer => (T*)_ptr;

    public T Value
    {
        readonly get => *(T*)_ptr;
        set => *(T*)_ptr = value;
    }

    private NativeValue(nint ptr) => _ptr = ptr;

    /// <summary>Allocates unmanaged memory for one <typeparamref name="T"/>, initialized to <paramref name="value"/>.</summary>
    public static NativeValue<T> Alloc(T value = default)
    {
        var ptr = (nint)NativeMemory.Alloc((nuint)sizeof(T));
        *(T*)ptr = value;
        return new NativeValue<T>(ptr);
    }

    public void Dispose()
    {
        if (_ptr == 0)
            return;

        NativeMemory.Free((void*)_ptr);
        _ptr = 0;
    }

    // Safe to keep implicit: this exposes a pointer to memory the instance already
    // owns — unlike the old design, nothing is allocated by this conversion, so a call
    // site using it can't accidentally acquire a resource it doesn't know it owns.
#pragma warning disable CA2225
    public static implicit operator T*(NativeValue<T> value) => value.Pointer;
#pragma warning restore CA2225
}

/// <summary>
/// Owns a contiguous unmanaged buffer of unmanaged values (a copy, not a pin — see
/// <see cref="PinnedHandle{T}"/> if you want zero-copy access to an existing managed array).
/// Same ref-struct lifetime discipline as <see cref="NativeValue{T}"/>.
/// </summary>
/// <typeparam name="T">The unmanaged element type.</typeparam>
public unsafe ref struct NativeArray<T> where T : unmanaged
{
    private nint _ptr;

    public readonly int Length { get; }
    public readonly int ByteLength => Length * sizeof(T);
    public readonly T* Pointer => (T*)_ptr;

    private NativeArray(nint ptr, int length)
    {
        _ptr = ptr;
        Length = length;
    }

    /// <summary>Allocates a copy of <paramref name="values"/> in unmanaged memory.</summary>
    public static NativeArray<T> Alloc(ReadOnlySpan<T> values)
    {
        // Two-arg NativeMemory.Alloc checks elementCount * elementSize for overflow
        // and throws instead of silently wrapping, unlike the old `Length * sizeof(T)` int math.
        var ptr = (nint)NativeMemory.Alloc((nuint)values.Length, (nuint)sizeof(T));
        values.CopyTo(new Span<T>((void*)ptr, values.Length));
        return new NativeArray<T>(ptr, values.Length);
    }

    /// <summary>Allocates an uninitialized buffer for <paramref name="length"/> elements — for native callees that write into it.</summary>
    public static NativeArray<T> AllocUninitialized(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        var ptr = (nint)NativeMemory.Alloc((nuint)length, (nuint)sizeof(T));
        return new NativeArray<T>(ptr, length);
    }

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

    public readonly Span<T> AsSpan() => new((T*)_ptr, Length);
    public readonly T[] ToArray() => AsSpan().ToArray();

    public void Dispose()
    {
        if (_ptr == 0)
            return;

        NativeMemory.Free((void*)_ptr);
        _ptr = 0;
    }

#pragma warning disable CA2225
    public static implicit operator T*(NativeArray<T> value) => value.Pointer;
#pragma warning restore CA2225
}

/// <summary>
/// Pins an EXISTING managed array in place (no copy) so native code can read/write it
/// directly — the true equivalent of `fixed (T* p = array)`, but with a disposable
/// lifetime instead of a lexical block. Not a ref struct: sometimes you legitimately
/// need to hold a pin across a field (e.g. a long-lived native callback buffer), and
/// GCHandle itself is safe to store — just be disciplined about calling Dispose.
/// </summary>
/// <typeparam name="T">The unmanaged element type of the pinned array.</typeparam>
public unsafe struct PinnedHandle<T> : IDisposable, IEquatable<PinnedHandle<T>> where T : unmanaged
{
    private GCHandle _handle;

    public readonly T* Pointer => _handle.IsAllocated ? (T*)_handle.AddrOfPinnedObject() : null;

    /// <summary>Pins <paramref name="array"/> in place. The array is NOT copied; writes through Pointer affect it directly.</summary>
    public PinnedHandle(T[] array)
    {
        ArgumentNullException.ThrowIfNull(array);
        _handle = GCHandle.Alloc(array, GCHandleType.Pinned);
    }

    public void Dispose()
    {
        if (_handle.IsAllocated)
            _handle.Free();
    }

    public readonly bool Equals(PinnedHandle<T> other) => _handle == other._handle;
    public readonly override bool Equals(object? obj) => obj is PinnedHandle<T> other && Equals(other);
    public readonly override int GetHashCode() => _handle.GetHashCode();
}

/// <summary>
/// Passes an arbitrary managed object through native code as an opaque pointer
/// (the classic "user data" / callback-context pattern) without pinning or copying it —
/// the object itself is free to move on the GC heap; only the handle is stable.
/// </summary>
public readonly struct ObjectHandle : IDisposable, IEquatable<ObjectHandle>
{
    private readonly GCHandle _handle;

    /// <summary>Opaque pointer to pass into native code as the callback context / user-data parameter.</summary>
    public nint Pointer => GCHandle.ToIntPtr(_handle);

    public ObjectHandle(object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _handle = GCHandle.Alloc(value);
    }

    /// <summary>Recovers the original managed object from the pointer handed back by native code.</summary>
    public static object FromPointer(nint pointer) => GCHandle.FromIntPtr(pointer).Target!;

    /// <summary>Typed convenience overload of <see cref="FromPointer(nint)"/>.</summary>
    public static T FromPointer<T>(nint pointer) where T : class => (T)FromPointer(pointer);

    public void Dispose()
    {
        if (_handle.IsAllocated)
            _handle.Free();
    }

    public bool Equals(ObjectHandle other) => _handle == other._handle;
    public override bool Equals(object? obj) => obj is ObjectHandle other && Equals(other);
    public override int GetHashCode() => _handle.GetHashCode();
}