// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Rebound.Core.Defrag;

public unsafe partial struct IDefragEngine : IComIID
{
    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT QueryInterface(Guid* riid, void** ppvObject)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, Guid*, void**, int>)
            (lpVtbl[0]))((IDefragEngine*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDefragEngine*, uint>)
            (lpVtbl[1]))((IDefragEngine*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDefragEngine*, uint>)
            (lpVtbl[2]))((IDefragEngine*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT Analyze(
        char* volumeName,
        Guid* param1,
        Guid* param2)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, char*, Guid*, Guid*, uint>)(lpVtbl[5]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), volumeName, param1, param2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT BootOptimize(
        ushort* volumeName,
        Guid* param1,
        Guid* param2)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, ushort*, Guid*, Guid*, uint>)(lpVtbl[6]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), volumeName, param1, param2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT Cancel(Guid id)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, Guid, uint>)(lpVtbl[7]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT DefragmentFull(
        ushort* volumeName,
        int flags,
        Guid* param1,
        Guid* param2)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, ushort*, int, Guid*, Guid*, uint>)(lpVtbl[8]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), volumeName, flags, param1, param2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT DefragmentFile(
        ushort* fileName,
        Guid* param)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, ushort*, Guid*, uint>)(lpVtbl[9]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), fileName, param);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT DefragmentSimple(
        ushort* volumePath,
        Guid* operationGuid,
        Guid* trackingGuid)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, ushort*, Guid*, Guid*, uint>)(lpVtbl[10]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), volumePath, operationGuid, trackingGuid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT SimpleOptimize(
        ushort* volumePath,
        Guid* operationGuid,
        Guid* trackingGuid)
            => DefragmentSimple(volumePath, operationGuid, trackingGuid);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT GetPossibleShrinkSpace(
        ulong param1,
        Guid* param2,
        ulong* param3)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, ulong, Guid*, ulong*, uint>)(lpVtbl[11]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), param1, param2, param3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT Consolidate(
        ulong param1,
        Guid* param2,
        ulong param3,
        ulong* param4)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, ulong, Guid*, ulong, ulong*, uint>)(lpVtbl[12]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), param1, param2, param3, param4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT Shrink(
        ulong param1,
        Guid* param2,
        void* param3,
        Guid* param4,
        ulong* param5)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, ulong, Guid*, void*, Guid*, ulong*, uint>)(lpVtbl[13]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), param1, param2, param3, param4, param5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT Retrim(
        ulong param1,
        Guid* param2,
        uint param3,
        ulong param4,
        ulong* param5)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, ulong, Guid*, uint, ulong, ulong*, uint>)(lpVtbl[14]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), param1, param2, param3, param4, param5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT Slabify(
        ulong param1,
        Guid* param2,
        uint param3,
        ulong param4,
        ulong* param5)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, ulong, Guid*, uint, ulong, ulong*, uint>)(lpVtbl[15]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), param1, param2, param3, param4, param5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT SlabifyRetrim(
        ulong param1,
        Guid* param2,
        uint param3,
        ulong param4,
        ulong* param5)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, ulong, Guid*, uint, ulong, ulong*, uint>)(lpVtbl[16]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), param1, param2, param3, param4, param5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT SlabAnalyze(
        ulong param1,
        Guid* param2,
        ulong param3,
        ulong* param4)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, ulong, Guid*, ulong, ulong*, uint>)(lpVtbl[17]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), param1, param2, param3, param4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LRESULT GetStatus(
        void** param1,
        void** param2,
        uint* param3,
        ulong* param4)
    {
        return (LRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, void**, void**, uint*, ulong*, long>)
            (lpVtbl[18]))((IDefragEngine*)Unsafe.AsPointer(ref this), param1, param2, param3, param4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LRESULT GetVolumeStatistics(
        ulong param1,
        void** param2,
        void** param3)
    {
        return (LRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, ulong, void**, void**, long>)(lpVtbl[19]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), param1, param2, param3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT Register(IDefragClient* client, Guid* param)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, IDefragClient*, Guid*, int>)(lpVtbl[20]))((IDefragEngine*)Unsafe.AsPointer(ref this), client, param);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LRESULT Unregister(long param1, long* param2)
    {
        return (LRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, long, long*, long>)(lpVtbl[21]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), param1, param2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT WaitForCompletion(
        long param1,
        void** param2,
        long* param3)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, long, void**, long*, int>)(lpVtbl[22]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), param1, param2, param3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT DisableAutomaticSleep(long param1, void** param2)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, long, void**, int>)(lpVtbl[23]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), param1, param2);
    }

    /// <summary>
    /// This function doesn't have clear variable names to be used easily.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LRESULT TierOptimize(
        ulong param1,
        uint* param2,
        ulong* param3)
    {
        return (LRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, ulong, uint*, ulong*, long>)(lpVtbl[24]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), param1, param2, param3);
    }

    /// <summary>
    /// This function doesn't have clear variable names to be used easily.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LRESULT BootOptimize2(
        ulong param1,
        Guid* param2,
        void** param3,
        ulong param4,
        uint* param5)
    {
        return (LRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, ulong, Guid*, void**, ulong, uint*, long>)(lpVtbl[25]))
            ((IDefragEngine*)Unsafe.AsPointer(ref this), param1, param2, param3, param4, param5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT Shutdown(long param)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, long, int>)(lpVtbl[26]))((IDefragEngine*)Unsafe.AsPointer(ref this), param);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT NotifyVolumeChange(long param)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, long, int>)(lpVtbl[27]))((IDefragEngine*)Unsafe.AsPointer(ref this), param);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT WaitForEvents()
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEngine*, int>)(lpVtbl[28]))((IDefragEngine*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IUnknown* GetControllingUnknown()
    {
        return (IUnknown*)((delegate* unmanaged[MemberFunction]<IDefragEngine*, int>)(lpVtbl[29]))((IDefragEngine*)Unsafe.AsPointer(ref this));
    }

    [GuidRVAGen.Guid("0C401E84-3083-4764-B6B5-A0DE8FEDD40C")]
    public static partial ref readonly Guid Guid { get; }
}
