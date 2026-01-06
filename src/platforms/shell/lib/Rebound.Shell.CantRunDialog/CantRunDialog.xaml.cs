// Copyright (C) Ivirius(TM) Community 2020 - 2026. All Rights Reserved.
// Licensed under the MIT License.

using System;
using CommunityToolkit.Mvvm.Input;
using Windows.UI.Xaml;
using Rebound.Core.Helpers;
using Windows.UI.Xaml.Controls;

namespace Rebound.Shell.CantRunDialog;

public sealed partial class CantRunDialog : Page
{
    private readonly Action? onClosedCallback;

    public CantRunDialog(Action? onClosed = null)
    {
        onClosedCallback = onClosed;
        InitializeComponent();
    }

    [RelayCommand]
    public void Cancel()
    {

    }
}