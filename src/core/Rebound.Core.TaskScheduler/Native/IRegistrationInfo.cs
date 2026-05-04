// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using TerraFX.Interop;
using TerraFX.Interop.Windows;

#pragma warning disable CA1051  // Do not declare visible instance fields
#pragma warning disable CA1815  // Override equals and operator equals on value types
#pragma warning disable CA1034  // Nested types should not be visible
#pragma warning disable CA1715  // Identifiers should have correct prefix
#pragma warning disable CA1707 // Identifiers should not contain underscores

namespace Rebound.Core.TaskScheduler.Native;

public unsafe partial struct IRegistrationInfo
{
    public partial struct Vtbl<TSelf> where TSelf : unmanaged, IUnknown.Interface
    {
        public IUnknown.Vtbl<TSelf> Base;

        public void* _disp0, _disp1, _disp2, _disp3;

        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Description;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_Description;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Author;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_Author;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Version;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_Version;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_double;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_double;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Documentation;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_Documentation;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_XmlText;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_XmlText;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_URI;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_URI;
        public delegate* unmanaged[MemberFunction]<TSelf*, VARIANT*, HRESULT> get_SecurityDescriptor;
        public delegate* unmanaged[MemberFunction]<TSelf*, VARIANT, HRESULT> put_SecurityDescriptor;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort**, HRESULT> get_Source;
        public delegate* unmanaged[MemberFunction]<TSelf*, ushort*, HRESULT> put_Source;
    }

    [GuidRVAGen.Guid("416D8B73-CB41-4EA1-805C-9BE9A5AC4A74")]
    public static partial Guid* IID { get; }
}

public unsafe partial struct IRegistrationInfo : IRegistrationInfo.Interface, INativeGuid
{
    public static Guid* NativeGuid => IID;
    public void** lpVtbl;

    public HRESULT QueryInterface(Guid* riid, void** ppvObject) =>
        ((delegate* unmanaged[MemberFunction]<IRegistrationInfo*, Guid*, void**, HRESULT>)lpVtbl[0])
            ((IRegistrationInfo*)Unsafe.AsPointer(in this), riid, ppvObject);

    public uint AddRef() =>
        ((delegate* unmanaged[MemberFunction]<IRegistrationInfo*, uint>)lpVtbl[1])
            ((IRegistrationInfo*)Unsafe.AsPointer(in this));

    public uint Release() =>
        ((delegate* unmanaged[MemberFunction]<IRegistrationInfo*, uint>)lpVtbl[2])
            ((IRegistrationInfo*)Unsafe.AsPointer(in this));

    public HRESULT get_Description(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IRegistrationInfo*, ushort**, HRESULT>)lpVtbl[7])
            ((IRegistrationInfo*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Description(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<IRegistrationInfo*, ushort*, HRESULT>)lpVtbl[8])
            ((IRegistrationInfo*)Unsafe.AsPointer(in this), v);

    public HRESULT get_Author(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IRegistrationInfo*, ushort**, HRESULT>)lpVtbl[9])
            ((IRegistrationInfo*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Author(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<IRegistrationInfo*, ushort*, HRESULT>)lpVtbl[10])
            ((IRegistrationInfo*)Unsafe.AsPointer(in this), v);

    public HRESULT get_Version(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IRegistrationInfo*, ushort**, HRESULT>)lpVtbl[11])
            ((IRegistrationInfo*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Version(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<IRegistrationInfo*, ushort*, HRESULT>)lpVtbl[12])
            ((IRegistrationInfo*)Unsafe.AsPointer(in this), v);

    public HRESULT get_double(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IRegistrationInfo*, ushort**, HRESULT>)lpVtbl[13])
            ((IRegistrationInfo*)Unsafe.AsPointer(in this), p);

    public HRESULT put_double(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<IRegistrationInfo*, ushort*, HRESULT>)lpVtbl[14])
            ((IRegistrationInfo*)Unsafe.AsPointer(in this), v);

    public HRESULT get_Documentation(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IRegistrationInfo*, ushort**, HRESULT>)lpVtbl[15])
            ((IRegistrationInfo*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Documentation(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<IRegistrationInfo*, ushort*, HRESULT>)lpVtbl[16])
            ((IRegistrationInfo*)Unsafe.AsPointer(in this), v);

    public HRESULT get_URI(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IRegistrationInfo*, ushort**, HRESULT>)lpVtbl[19])
            ((IRegistrationInfo*)Unsafe.AsPointer(in this), p);

    public HRESULT put_URI(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<IRegistrationInfo*, ushort*, HRESULT>)lpVtbl[20])
            ((IRegistrationInfo*)Unsafe.AsPointer(in this), v);

    public HRESULT get_Source(ushort** p) =>
        ((delegate* unmanaged[MemberFunction]<IRegistrationInfo*, ushort**, HRESULT>)lpVtbl[23])
            ((IRegistrationInfo*)Unsafe.AsPointer(in this), p);

    public HRESULT put_Source(ushort* v) =>
        ((delegate* unmanaged[MemberFunction]<IRegistrationInfo*, ushort*, HRESULT>)lpVtbl[24])
            ((IRegistrationInfo*)Unsafe.AsPointer(in this), v);

    public interface Interface : IUnknown.Interface
    {
        HRESULT get_Description(ushort** p);
        HRESULT put_Description(ushort* v);
        HRESULT get_Author(ushort** p);
        HRESULT put_Author(ushort* v);
        HRESULT get_Version(ushort** p);
        HRESULT put_Version(ushort* v);
        HRESULT get_double(ushort** p);
        HRESULT put_double(ushort* v);
        HRESULT get_Documentation(ushort** p);
        HRESULT put_Documentation(ushort* v);
        HRESULT get_URI(ushort** p);
        HRESULT put_URI(ushort* v);
        HRESULT get_Source(ushort** p);
        HRESULT put_Source(ushort* v);
    }
}
