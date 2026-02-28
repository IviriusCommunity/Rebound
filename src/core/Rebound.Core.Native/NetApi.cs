// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Rebound.Core.Native;

[StructLayout(LayoutKind.Sequential)]
#pragma warning disable CA1707
public unsafe struct USER_INFO_2 : IEquatable<USER_INFO_2>
#pragma warning restore CA1707
{
    public char* usri2_name;
    public char* usri2_password;
    public uint usri2_password_age;
    public uint usri2_priv;
    public char* usri2_home_dir;
    public char* usri2_comment;
    public uint usri2_flags;
    public char* usri2_script_path;
    public uint usri2_auth_flags;
    public char* usri2_full_name;
    public char* usri2_usr_comment;
    public char* usri2_parms;
    public char* usri2_workstations;
    public uint usri2_last_logon;
    public uint usri2_last_logoff;
    public uint usri2_acct_expires;
    public uint usri2_max_storage;
    public uint usri2_units_per_week;
    public Windows.Win32.Foundation.HANDLE usri2_logon_hours;
    public uint usri2_bad_pw_count;
    public uint usri2_num_logons;
    public char* usri2_logon_server;
    public uint usri2_country_code;
    public uint usri2_code_page;

    public readonly bool Equals(USER_INFO_2 other) =>
        usri2_name == other.usri2_name &&
        usri2_password == other.usri2_password &&
        usri2_password_age == other.usri2_password_age &&
        usri2_priv == other.usri2_priv &&
        usri2_home_dir == other.usri2_home_dir &&
        usri2_comment == other.usri2_comment &&
        usri2_flags == other.usri2_flags &&
        usri2_script_path == other.usri2_script_path &&
        usri2_auth_flags == other.usri2_auth_flags &&
        usri2_full_name == other.usri2_full_name &&
        usri2_usr_comment == other.usri2_usr_comment &&
        usri2_parms == other.usri2_parms &&
        usri2_workstations == other.usri2_workstations &&
        usri2_last_logon == other.usri2_last_logon &&
        usri2_last_logoff == other.usri2_last_logoff &&
        usri2_acct_expires == other.usri2_acct_expires &&
        usri2_max_storage == other.usri2_max_storage &&
        usri2_units_per_week == other.usri2_units_per_week &&
        usri2_logon_hours == other.usri2_logon_hours &&
        usri2_bad_pw_count == other.usri2_bad_pw_count &&
        usri2_num_logons == other.usri2_num_logons &&
        usri2_logon_server == other.usri2_logon_server &&
        usri2_country_code == other.usri2_country_code &&
        usri2_code_page == other.usri2_code_page;

    public readonly override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is USER_INFO_2 other && Equals(other);

    public readonly override int GetHashCode() =>
        HashCode.Combine(
            HashCode.Combine(
                (nint)usri2_name,
                (nint)usri2_password,
                usri2_password_age,
                usri2_priv,
                (nint)usri2_home_dir,
                (nint)usri2_comment,
                usri2_flags,
                (nint)usri2_script_path),
            HashCode.Combine(
                usri2_auth_flags,
                (nint)usri2_full_name,
                (nint)usri2_usr_comment,
                (nint)usri2_parms,
                (nint)usri2_workstations,
                usri2_last_logon,
                usri2_last_logoff,
                usri2_acct_expires),
            HashCode.Combine(
                usri2_max_storage,
                usri2_units_per_week,
                usri2_bad_pw_count,
                usri2_num_logons,
                (nint)usri2_logon_server,
                usri2_country_code,
                usri2_code_page));

    public static bool operator ==(USER_INFO_2 left, USER_INFO_2 right) => left.Equals(right);
    public static bool operator !=(USER_INFO_2 left, USER_INFO_2 right) => !left.Equals(right);
}

[StructLayout(LayoutKind.Sequential)]
#pragma warning disable CA1707
public struct USER_MODALS_INFO_0 : IEquatable<USER_MODALS_INFO_0>
#pragma warning restore CA1707
{
    public uint usrmod0_min_passwd_len;
    public uint usrmod0_max_passwd_age;
    public uint usrmod0_min_passwd_age;
    public uint usrmod0_force_logoff;
    public uint usrmod0_password_hist_len;

    public readonly bool Equals(USER_MODALS_INFO_0 other) =>
        usrmod0_min_passwd_len == other.usrmod0_min_passwd_len &&
        usrmod0_max_passwd_age == other.usrmod0_max_passwd_age &&
        usrmod0_min_passwd_age == other.usrmod0_min_passwd_age &&
        usrmod0_force_logoff == other.usrmod0_force_logoff &&
        usrmod0_password_hist_len == other.usrmod0_password_hist_len;

    public readonly override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is USER_MODALS_INFO_0 other && Equals(other);

    public readonly override int GetHashCode() =>
        HashCode.Combine(usrmod0_min_passwd_len, usrmod0_max_passwd_age, usrmod0_min_passwd_age, usrmod0_force_logoff, usrmod0_password_hist_len);

    public static bool operator ==(USER_MODALS_INFO_0 left, USER_MODALS_INFO_0 right) => left.Equals(right);
    public static bool operator !=(USER_MODALS_INFO_0 left, USER_MODALS_INFO_0 right) => !left.Equals(right);
}

public static partial class NetApi
{
    [LibraryImport("netapi32.dll", EntryPoint = "NetUserGetInfo")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    private static unsafe partial uint NetUserGetInfo_Impl(
        char* servername,
        char* username,
        uint level,
        byte** bufptr);

    public static unsafe HRESULT NetUserGetInfo(
        char* servername,
        char* username,
        uint level,
        byte** bufptr)
    {
        return new((int)NetUserGetInfo_Impl(servername, username, level, bufptr));
    }

    [LibraryImport("netapi32.dll", EntryPoint = "NetUserModalsGet")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    private static unsafe partial uint NetUserModalsGet_Impl(
        char* servername,
        uint level,
        byte** bufptr);

    public static unsafe HRESULT NetUserModalsGet(
        char* servername,
        uint level,
        byte** bufptr)
    {
        return new((int)NetUserModalsGet_Impl(servername, level, bufptr));
    }

    [LibraryImport("netapi32.dll", EntryPoint = "NetApiBufferFree")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvStdcall)])]
    private static unsafe partial uint NetApiBufferFree_Impl(void* buffer);

    public static unsafe HRESULT NetApiBufferFree(void* buffer)
    {
        return new((int)NetApiBufferFree_Impl(buffer));
    }
}