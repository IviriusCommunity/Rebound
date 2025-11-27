// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Rebound.Core.Defrag;

public unsafe partial struct IDefragClient : IComIID
{
    public void** lpVtbl;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HRESULT ChangeNotification(UInt64 notificationId, UInt32 notificationType, void* notificationData)
    {
        return (HRESULT)((delegate* unmanaged[MemberFunction]<IDefragClient*, UInt64, UInt32, void*, int>)(lpVtbl[3]))((IDefragClient*)Unsafe.AsPointer(ref this), notificationId, notificationType, notificationData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IUnknown* GetControllingUnknown()
    {
        return (IUnknown*)((delegate* unmanaged[MemberFunction]<IDefragClient*, int>)(lpVtbl[4]))((IDefragClient*)Unsafe.AsPointer(ref this));
    }

    [GuidRVAGen.Guid("c958543e-b3a0-46ee-8085-4f111910d401")]
    public static partial ref readonly Guid Guid { get; }
}
