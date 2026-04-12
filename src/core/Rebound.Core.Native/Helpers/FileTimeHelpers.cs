// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using TerraFX.Interop.Windows;

namespace Rebound.Core.Native.Helpers;

public static class FileTimeHelpers
{
    public static ulong FileTimeToUlong(FILETIME ft)
        => ((ulong)ft.dwHighDateTime << 32) | ft.dwLowDateTime;
}