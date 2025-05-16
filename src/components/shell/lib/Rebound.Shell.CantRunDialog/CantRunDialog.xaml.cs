// Copyright (C) Ivirius(TM) Community 2020 - 2025. All Rights Reserved.
// Licensed under the MIT License.

using System;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Rebound.Helpers;
using WinUIEx;

namespace Rebound.Shell.CantRunDialog;

public sealed partial class CantRunDialog : WindowEx
{
    private readonly Action? onClosedCallback;

    public CantRunDialog(Action? onClosed = null)
    {
        onClosedCallback = onClosed;
        InitializeComponent();
        this.CenterOnScreen();
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Collapsed;
    }

    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        onClosedCallback?.Invoke();
    }

    private void WindowEx_Activated(object sender, WindowActivatedEventArgs args)
    {
        this.SetTaskBarIcon(Icon.FromFile($"{AppContext.BaseDirectory}\\Assets\\Rebound.ico"));
    }

    [RelayCommand]
    public void Cancel()
    {
        Close();
    }
}