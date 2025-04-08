// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Rebound.Helpers;
using System;
using WinUIEx;

namespace Rebound.Shell.Run;

public sealed partial class RunWindow : WindowEx
{
    private readonly Action? onClosedCallback;

    public RunWindow(Action? onClosed = null)
    {
        onClosedCallback = onClosed;
        InitializeComponent();
        var scale = Display.GetScale(this);
        this.Move((int)(25 * scale), (int)(Display.GetDPIAwareDisplayRect(this).Height - (48 + 25) * scale - Height * scale));
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        onClosedCallback?.Invoke();
    }

    private void WindowEx_Activated(object sender, WindowActivatedEventArgs args)
    {
        this.SetTaskBarIcon(Icon.FromFile($"{AppContext.BaseDirectory}\\Assets\\RunBox.ico"));
        this.RemoveIcon();
    }
}