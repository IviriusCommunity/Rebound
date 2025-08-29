// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Windowing;

namespace Rebound.Core.Helpers.Windowing;

public static class WindowHelper
{
    public static void SetWindowIcon(this AppWindow window, string iconPath)
    {
        window?.SetTitleBarIcon(iconPath);
        window?.SetTaskbarIcon(iconPath);
    }
}