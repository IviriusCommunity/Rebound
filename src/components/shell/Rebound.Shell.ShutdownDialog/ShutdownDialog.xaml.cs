// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using WinUIEx;

namespace Rebound.Shell.ShutdownDialog;

public sealed partial class ShutdownDialog : WindowEx
{
    private readonly Action? onClosedCallback;

    private WindowManager? windowManager;

    private const int WM_ACTIVATE = 0x0006;
    private const int WA_INACTIVE = 0;

    public ShutdownDialog(Action? onClosed = null)
    {
        onClosedCallback = onClosed;
        InitializeComponent();
    }

    private void Manager_WindowMessageReceived(object? sender, WinUIEx.Messaging.WindowMessageEventArgs e)
    {
        /*if (e.Message.MessageId == WM_ACTIVATE && e.Message.WParam == WA_INACTIVE) 
            Close();*/
    }

    private unsafe void WindowEx_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        windowManager = null;
        onClosedCallback?.Invoke();
    }

    private void WindowEx_Activated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
    {
        //this.CenterOnScreen();
        if (windowManager == null)
        {
            windowManager = WindowManager.Get(this);
            windowManager.WindowMessageReceived += Manager_WindowMessageReceived;
        }
    }
}
