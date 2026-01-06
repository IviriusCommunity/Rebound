// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Rebound.Core.Defrag;

/// <summary>
/// IDefragClient - Callback interface for receiving notifications
/// Used as secondary interface in COperationTracker
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe partial struct IDefragClient : IComIID
{
    public void** lpVtbl;

    #region IUnknown Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT QueryInterface(Guid* riid, void** ppvObject)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragClient*, Guid*, void**, int>)
            (lpVtbl[0]))((IDefragClient*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDefragClient*, uint>)
            (lpVtbl[1]))((IDefragClient*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDefragClient*, uint>)
            (lpVtbl[2]))((IDefragClient*)Unsafe.AsPointer(ref this));
    }

    #endregion

    #region IDefragClient Methods

    /// <summary>
    /// Receive change notification from defrag engine (vtable offset 0x18)
    /// </summary>
    /// <param name="sequenceNumber">Notification sequence number (for ordering)</param>
    /// <param name="statusCount">Number of status structures</param>
    /// <param name="statusArray">Array of defrag status structures</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT ChangeNotification(ulong sequenceNumber, uint statusCount, void* statusArray)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragClient*, ulong, uint, void*, int>)
            (lpVtbl[3]))((IDefragClient*)Unsafe.AsPointer(ref this), sequenceNumber, statusCount, statusArray);
    }

    /// <summary>
    /// Called when status changes (vtable offset 0x20)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT OnStatusUpdate(int statusCode)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragClient*, int, int>)
            (lpVtbl[4]))((IDefragClient*)Unsafe.AsPointer(ref this), statusCode);
    }

    #endregion

    // Note: GUID not in string table, need to find from registry or binary
    public static ref readonly Guid Guid => throw new NotImplementedException("GUID needs to be extracted");
}
