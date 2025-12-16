// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Rebound.Core.Defrag;

public unsafe partial struct IDefragEnginePriv : IComIID
{
    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT Analyze(
        char* volumeName,
        Guid* param1,
        Guid* param2)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, char*, Guid*, Guid*, uint>)(lpVtbl[5]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), volumeName, param1, param2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT BootOptimize(
        ushort* volumeName,
        Guid* param1,
        Guid* param2)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ushort*, Guid*, Guid*, uint>)(lpVtbl[6]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), volumeName, param1, param2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT Cancel(Guid id)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, Guid, uint>)(lpVtbl[7]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT DefragmentFull(
        ushort* volumeName,
        int flags,
        Guid* param1,
        Guid* param2)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ushort*, int, Guid*, Guid*, uint>)(lpVtbl[8]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), volumeName, flags, param1, param2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT DefragmentFile(
        ushort* fileName,
        Guid* param)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ushort*, Guid*, uint>)(lpVtbl[9]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), fileName, param);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT DefragmentSimple(
        ulong param1,
        void* param2)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, void*, uint>)(lpVtbl[10]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT GetPossibleShrinkSpace(
        ulong param1,
        Guid* param2,
        ulong* param3)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, Guid*, ulong*, uint>)(lpVtbl[11]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT Consolidate(
        ulong param1,
        Guid* param2,
        ulong param3,
        ulong* param4)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, Guid*, ulong, ulong*, uint>)(lpVtbl[12]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3, param4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT Shrink(
        ulong param1,
        Guid* param2,
        void* param3,
        Guid* param4,
        ulong* param5)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, Guid*, void*, Guid*, ulong*, uint>)(lpVtbl[13]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3, param4, param5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT Retrim(
        ulong param1,
        Guid* param2,
        uint param3,
        ulong param4,
        ulong* param5)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, Guid*, uint, ulong, ulong*, uint>)(lpVtbl[14]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3, param4, param5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT Slabify(
        ulong param1,
        Guid* param2,
        uint param3,
        ulong param4,
        ulong* param5)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, Guid*, uint, ulong, ulong*, uint>)(lpVtbl[15]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3, param4, param5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT SlabifyRetrim(
        ulong param1,
        Guid* param2,
        uint param3,
        ulong param4,
        ulong* param5)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, Guid*, uint, ulong, ulong*, uint>)(lpVtbl[16]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3, param4, param5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT SlabAnalyze(
        ulong param1,
        Guid* param2,
        ulong param3,
        ulong* param4)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, Guid*, ulong, ulong*, uint>)(lpVtbl[17]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3, param4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LRESULT GetStatus(
        void** param1,
        void** param2,
        uint* param3,
        ulong* param4)
    {
        return (LRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, void**, void**, uint*, ulong*, long>)
            (lpVtbl[18]))((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3, param4);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LRESULT GetVolumeStatistics(
        ulong param1,
        void** param2,
        void** param3)
    {
        return (LRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, void**, void**, long>)(lpVtbl[19]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT Register(IDefragClient* client, Guid* param)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, IDefragClient*, Guid*, int>)(lpVtbl[20]))((IDefragEnginePriv*)Unsafe.AsPointer(ref this), client, param);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LRESULT Unregister(long param1, long* param2)
    {
        return (LRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, long, long*, long>)(lpVtbl[21]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT WaitForCompletion(
        long param1,
        void** param2,
        long* param3)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, long, void**, long*, int>)(lpVtbl[22]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT DisableAutomaticSleep(long param1, void** param2)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, long, void**, int>)(lpVtbl[23]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2);
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
        return (LRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, uint*, ulong*, long>)(lpVtbl[24]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3);
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
        return (LRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, ulong, Guid*, void**, ulong, uint*, long>)(lpVtbl[25]))
            ((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param1, param2, param3, param4, param5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT Shutdown(long param)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, long, int>)(lpVtbl[26]))((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT NotifyVolumeChange(long param)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, long, int>)(lpVtbl[27]))((IDefragEnginePriv*)Unsafe.AsPointer(ref this), param);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT WaitForEvents()
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, int>)(lpVtbl[28]))((IDefragEnginePriv*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IUnknown* GetControllingUnknown()
    {
        return (IUnknown*)((delegate* unmanaged[MemberFunction]<IDefragEnginePriv*, int>)(lpVtbl[29]))((IDefragEnginePriv*)Unsafe.AsPointer(ref this));
    }

    [GuidRVAGen.Guid("759b45ae-5c6d-4e1d-97a6-7aa7408c1787")]
    public static partial ref readonly Guid Guid { get; }
}
