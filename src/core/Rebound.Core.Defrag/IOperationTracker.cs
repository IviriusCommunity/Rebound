// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Rebound.Core.Defrag;

/// <summary>
/// IOperationTracker - Main interface for tracking defragmentation operations
/// GUID: 81a4d1fa-4fc8-4e1f-88da-cc7edf7482ee
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe partial struct IOperationTracker : IComIID
{
    public void** lpVtbl;

    #region IUnknown Methods (vtable offset 0x00, 0x08, 0x10)

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT QueryInterface(Guid* riid, void** ppvObject)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IOperationTracker*, Guid*, void**, int>)
            (lpVtbl[0]))((IOperationTracker*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IOperationTracker*, uint>)
            (lpVtbl[1]))((IOperationTracker*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IOperationTracker*, uint>)
            (lpVtbl[2]))((IOperationTracker*)Unsafe.AsPointer(ref this));
    }

    #endregion

    #region IOperationTracker Methods

    /// <summary>
    /// Get controlling IUnknown (vtable offset 0x18)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IUnknown* GetControllingUnknown()
    {
        return (IUnknown*)((delegate* unmanaged[MemberFunction]<IOperationTracker*, IUnknown*>)
            (lpVtbl[3]))((IOperationTracker*)Unsafe.AsPointer(ref this));
    }

    /// <summary>
    /// Invoke a defragmentation operation (vtable offset 0x20)
    /// </summary>
    /// <param name="operationType">Type of operation (1=Analyze, 2=Defrag, 3=Optimize, etc.)</param>
    /// <param name="volumePath">Path to volume (e.g., "C:")</param>
    /// <param name="options">Operation options structure</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT InvokeOperation(uint operationType, ushort* volumePath, void* options)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IOperationTracker*, uint, ushort*, void*, int>)
            (lpVtbl[4]))((IOperationTracker*)Unsafe.AsPointer(ref this), operationType, volumePath, options);
    }

    /// <summary>
    /// Track operation progress (blocks until complete) (vtable offset 0x28)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT TrackOperation()
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IOperationTracker*, int>)
            (lpVtbl[5]))((IOperationTracker*)Unsafe.AsPointer(ref this));
    }

    /// <summary>
    /// Shutdown and optionally cancel operation (vtable offset 0x30)
    /// </summary>
    /// <param name="forceCancel">1 to cancel, 0 to wait for completion</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT Shutdown(int forceCancel = 0)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IOperationTracker*, int, int>)
            (lpVtbl[6]))((IOperationTracker*)Unsafe.AsPointer(ref this), forceCancel);
    }

    /// <summary>
    /// Get status for specific volume (vtable offset 0x38)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT GetStatus(ushort* volumePath, void** statusArray)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IOperationTracker*, ushort*, void**, int>)
            (lpVtbl[7]))((IOperationTracker*)Unsafe.AsPointer(ref this), volumePath, statusArray);
    }

    /// <summary>
    /// Get all operation statuses (vtable offset 0x40)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT GetAllStatus(uint* count, void** statusArray)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IOperationTracker*, uint*, void**, int>)
            (lpVtbl[8]))((IOperationTracker*)Unsafe.AsPointer(ref this), count, statusArray);
    }

    /// <summary>
    /// Get operation GUID (vtable offset 0x48)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT GetOperationID(Guid* operationId)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IOperationTracker*, Guid*, int>)
            (lpVtbl[9]))((IOperationTracker*)Unsafe.AsPointer(ref this), operationId);
    }

    /// <summary>
    /// Get friendly name for volume (vtable offset 0x50)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT GetFriendlyName(ushort* volumePath, ushort** friendlyName)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IOperationTracker*, ushort*, ushort**, int>)
            (lpVtbl[10]))((IOperationTracker*)Unsafe.AsPointer(ref this), volumePath, friendlyName);
    }

    /// <summary>
    /// Print volume statistics (vtable offset 0x58)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT PrintVolumeStatistics(uint operationType)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IOperationTracker*, uint, int>)
            (lpVtbl[11]))((IOperationTracker*)Unsafe.AsPointer(ref this), operationType);
    }

    /// <summary>
    /// Detach tracker with result code (vtable offset 0x60)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT DetachTracker(int result)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IOperationTracker*, int, int>)
            (lpVtbl[12]))((IOperationTracker*)Unsafe.AsPointer(ref this), result);
    }

    #endregion

    [GuidRVAGen.Guid("81a4d1fa-4fc8-4e1f-88da-cc7edf7482ee")]
    public static partial ref readonly Guid Guid { get; }
}

/// <summary>
/// Defragmentation operation options
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct DefragOperationOptions
{
    // Offset 0x00: Operation-specific flags
    public uint Flags;

    // Offset 0x04: Priority (0=low, 1=normal, 2=high)
    public uint Priority;

    // Offset 0x08: Whether to use verbose output
    public int VerboseOutput;

    // Offset 0x0C: Timeout in milliseconds
    public uint TimeoutMs;

    // Offset 0x10: JSON output mode flag
    public int JsonOutput;

    // Offset 0x14-0x1F: Reserved/padding
    private fixed byte Reserved[12];

    // Offset 0x20: Additional callback interface (optional)
    public void* CallbackInterface;

    // Offset 0x28: Custom data pointer (optional)
    public void* CustomData;

    // Offset 0x30: Use simple optimization (for operation type 3)
    public int UseSimpleOptimization;

    // Offset 0x34-0x3F: Operation-specific parameters
    public ulong OperationFlags;
    public ulong RescanFlags;

    // Offset 0x48: Volume-specific flags
    public ulong VolumeFlags;
}

/// <summary>
/// Operation type enumeration
/// </summary>
public enum DefragOperationType : uint
{
    Analyze = 1,                    // Analyze volume fragmentation
    Defragment = 2,                 // Full defragmentation
    Optimize = 3,                   // Optimize (can be simple or full)
    ConsolidateFreeSpace = 4,       // Consolidate free space
    Rescan = 5,                     // Rescan volume
    SlabConsolidation = 6,          // Slab consolidation
    TierOptimization = 7,           // Tiered storage optimization
    Retrim = 8,                     // TRIM/unmap operation
    ThinProvisioning = 9,           // Thin provisioning optimization
    TieredStorage = 10,             // Advanced tiered storage
}

/// <summary>
/// Helper extension methods for IOperationTracker
/// </summary>
public static unsafe class IOperationTrackerExtensions
{
    /// <summary>
    /// Convenience method to invoke operation with managed string
    /// </summary>
    public static HRESULT InvokeOperation(
        this ref IOperationTracker tracker,
        DefragOperationType operationType,
        string volumePath,
        DefragOperationOptions options)
    {
        fixed (char* pPath = volumePath)
        {
            return tracker.InvokeOperation((uint)operationType, (ushort*)pPath, &options);
        }
    }

    /// <summary>
    /// Get operation ID as managed Guid
    /// </summary>
    public static HRESULT GetOperationID(this ref IOperationTracker tracker, out Guid operationId)
    {
        operationId = Guid.Empty;
        fixed (Guid* pGuid = &operationId)
        {
            return tracker.GetOperationID(pGuid);
        }
    }

    /// <summary>
    /// Get friendly name as managed string
    /// </summary>
    public static HRESULT GetFriendlyName(
        this ref IOperationTracker tracker,
        string volumePath,
        out string friendlyName)
    {
        friendlyName = string.Empty;
        ushort* pName = null;

        fixed (char* pPath = volumePath)
        {
            var hr = tracker.GetFriendlyName((ushort*)pPath, &pName);
            if (hr.Succeeded && pName != null)
            {
                friendlyName = new string((char*)pName);
                Marshal.FreeCoTaskMem((IntPtr)pName);
            }
            return hr;
        }
    }
}