// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

namespace Rebound.Core.Native.TerraFX;

public static class FILETIME
{
    public static DateTime ToDateTime(this global::TerraFX.Interop.Windows.FILETIME ft)
        => DateTime.FromFileTime(((long)ft.dwHighDateTime << 32) | ft.dwLowDateTime);

    public static long ToLong(this global::TerraFX.Interop.Windows.FILETIME ft)
        => ((long)ft.dwHighDateTime << 32) | ft.dwLowDateTime;
}