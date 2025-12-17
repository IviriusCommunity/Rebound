// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Rebound.Core.Defrag;

[StructLayout(LayoutKind.Sequential)]
public unsafe partial struct IDefragmentSimple2 : IComIID
{
    public void** lpVtbl;

    #region IUnknown Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT QueryInterface(Guid* riid, void** ppvObject)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragmentSimple2*, Guid*, void**, int>)
            (lpVtbl[0]))((IDefragmentSimple2*)Unsafe.AsPointer(ref this), riid, ppvObject);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint AddRef()
    {
        return ((delegate* unmanaged[MemberFunction]<IDefragmentSimple2*, uint>)
            (lpVtbl[1]))((IDefragmentSimple2*)Unsafe.AsPointer(ref this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Release()
    {
        return ((delegate* unmanaged[MemberFunction]<IDefragmentSimple2*, uint>)
            (lpVtbl[2]))((IDefragmentSimple2*)Unsafe.AsPointer(ref this));
    }

    #endregion

    #region IDefragmentSimple2 Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT DefragmentSimple2(ushort* volumePath, int priority, ushort* normalizedPath, Guid* operationGuid, Guid* trackingGuid)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragmentSimple2*, ushort*, int, ushort*, Guid*, Guid*, int>)
            (lpVtbl[3]))((IDefragmentSimple2*)Unsafe.AsPointer(ref this), volumePath, priority, normalizedPath, operationGuid, trackingGuid);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT StartSimpleOptimize(
        ushort* volumePath,
        int priority,
        ushort* normalizedPath,
        Guid* operationGuid,
        Guid* trackingGuid)
            => DefragmentSimple2(volumePath, priority, normalizedPath, operationGuid, trackingGuid);

    #endregion

    [GuidRVAGen.Guid("5a43b3be-3deb-11ed-b878-0242ac120002")]
    public static partial ref readonly Guid Guid { get; }
}
